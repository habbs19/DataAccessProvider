using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Types;

namespace DataAccessProvider.Core.Tests;

public class DatabaseTransactionTests
{
    [Fact]
    public async Task Success_UsesOneTransactionForAllCommandKinds_AndCommits()
    {
        var connection = new RecordingConnection();
        var source = new FakeSource(connection);

        var callbackResult = await source.ExecuteInTransactionAsync(
            async (transaction, ct) =>
            {
                await transaction.ExecuteNonQueryAsync(new FakeSourceParams(), ct);
                await transaction.ExecuteScalarAsync(new FakeSourceParams(), ct);
                await transaction.ExecuteReaderAsync(new FakeSourceParams(), ct);
                var typed = await transaction.ExecuteReaderAsync<Person, FakeSourceParams<Person>>(
                    new FakeSourceParams<Person>(), ct);

                Assert.Equal("Ada", Assert.Single(typed.Value!).Name);
                return "committed";
            },
            IsolationLevel.Serializable);

        Assert.Equal("committed", callbackResult);
        Assert.Equal(1, connection.OpenCount);
        Assert.Equal(IsolationLevel.Serializable, connection.Transaction.IsolationLevel);
        Assert.Equal(1, connection.Transaction.CommitCount);
        Assert.Equal(0, connection.Transaction.RollbackCount);
        Assert.Equal(4, connection.Commands.Count);
        Assert.All(connection.Commands, command => Assert.Same(connection.Transaction, command.AssignedTransaction));
        Assert.True(connection.IsDisposed);
        Assert.True(connection.Transaction.IsDisposed);
    }

    [Fact]
    public async Task DefaultIsolation_UsesProviderDefault_AndDoesNotInvokeResiliencePolicy()
    {
        var connection = new RecordingConnection();
        var resiliencePolicy = new CountingResiliencePolicy();
        var source = new FakeSource(connection, resiliencePolicy);

        await source.ExecuteInTransactionAsync((_, _) => Task.CompletedTask);

        Assert.Equal(IsolationLevel.Unspecified, connection.Transaction.IsolationLevel);
        Assert.Equal(0, resiliencePolicy.ExecutionCount);
    }

    [Fact]
    public async Task OperationFailure_RollsBack_AndPreservesOriginalException()
    {
        var connection = new RecordingConnection();
        var source = new FakeSource(connection);
        var original = new InvalidOperationException("operation failed");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.ExecuteInTransactionAsync((_, _) => Task.FromException(original)));

        Assert.Same(original, thrown);
        Assert.Equal(0, connection.Transaction.CommitCount);
        Assert.Equal(1, connection.Transaction.RollbackCount);
    }

    [Fact]
    public async Task RollbackFailure_PreservesOperationAndRollbackFailures()
    {
        var connection = new RecordingConnection();
        var source = new FakeSource(connection);
        var operationFailure = new InvalidOperationException("operation failed");
        var rollbackFailure = new ApplicationException("rollback failed");
        connection.Transaction.RollbackException = rollbackFailure;

        var thrown = await Assert.ThrowsAsync<DatabaseTransactionException>(() =>
            source.ExecuteInTransactionAsync((_, _) => Task.FromException(operationFailure)));

        Assert.Equal(DatabaseTransactionFailureStage.Rollback, thrown.FailureStage);
        Assert.Same(operationFailure, thrown.PrimaryException);
        Assert.Same(rollbackFailure, thrown.RollbackException);
        Assert.False(thrown.CommitOutcomeUnknown);
    }

    [Fact]
    public async Task CommitFailure_IsReportedAsUnknownOutcome_AndIsNotRetried()
    {
        var connection = new RecordingConnection();
        var source = new FakeSource(connection);
        var commitFailure = new InvalidOperationException("commit failed");
        connection.Transaction.CommitException = commitFailure;

        var thrown = await Assert.ThrowsAsync<DatabaseTransactionException>(() =>
            source.ExecuteInTransactionAsync((_, _) => Task.CompletedTask));

        Assert.Equal(DatabaseTransactionFailureStage.Commit, thrown.FailureStage);
        Assert.Same(commitFailure, thrown.PrimaryException);
        Assert.True(thrown.CommitOutcomeUnknown);
        Assert.Equal(1, connection.Transaction.CommitCount);
        Assert.Equal(1, connection.Transaction.RollbackCount);
    }

    [Fact]
    public async Task CancellationBeforeCommit_RollsBack()
    {
        var connection = new RecordingConnection();
        var source = new FakeSource(connection);
        using var cancellation = new CancellationTokenSource();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            source.ExecuteInTransactionAsync(
                (_, _) =>
                {
                    cancellation.Cancel();
                    return Task.CompletedTask;
                },
                cancellationToken: cancellation.Token));

        Assert.Equal(0, connection.Transaction.CommitCount);
        Assert.Equal(1, connection.Transaction.RollbackCount);
    }

    [Fact]
    public async Task ExecutorRejectsUseAfterCallbackAndCrossProviderParameters()
    {
        var connection = new RecordingConnection();
        var source = new FakeSource(connection);
        IDatabaseTransaction? escaped = null;

        await source.ExecuteInTransactionAsync((transaction, _) =>
        {
            escaped = transaction;
            return Task.CompletedTask;
        });

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            escaped!.ExecuteNonQueryAsync(new FakeSourceParams()));

        var mismatchConnection = new RecordingConnection();
        var mismatchSource = new FakeSource(mismatchConnection);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            mismatchSource.ExecuteInTransactionAsync(
                (transaction, ct) => transaction.ExecuteNonQueryAsync(new OtherSourceParams(), ct)));
        Assert.Equal(1, mismatchConnection.Transaction.RollbackCount);
    }

    [Fact]
    public async Task ConcurrentCommands_AreSerialized()
    {
        var connection = new RecordingConnection { CommandDelay = TimeSpan.FromMilliseconds(30) };
        var source = new FakeSource(connection);

        await source.ExecuteInTransactionAsync(async (transaction, ct) =>
        {
            var first = transaction.ExecuteNonQueryAsync(new FakeSourceParams(), ct);
            var second = transaction.ExecuteNonQueryAsync(new FakeSourceParams(), ct);
            await Task.WhenAll(first, second);
        });

        Assert.Equal(1, connection.MaximumConcurrentCommands);
    }

    private sealed class Person
    {
        public string Name { get; set; } = string.Empty;
    }

    private class FakeSourceParams : BaseDatabaseSourceParams;

    private sealed class FakeSourceParams<TValue> : BaseDatabaseSourceParams<TValue>
        where TValue : class;

    private sealed class OtherSourceParams : BaseDatabaseSourceParams;

    private sealed class FakeSource : BaseDatabaseSource<FakeSourceParams>,
        IDatabaseTransactionProvider<FakeSourceParams>
    {
        private readonly RecordingConnection _connection;

        public FakeSource(RecordingConnection connection, IResiliencePolicy? resiliencePolicy = null)
            : base("fake", resiliencePolicy) => _connection = connection;

        public override DbConnection GetConnection() => _connection;

        public override DbCommand GetCommand(string query, DbConnection connection)
        {
            var command = new RecordingCommand((RecordingConnection)connection) { CommandText = query };
            _connection.Commands.Add(command);
            return command;
        }

        protected override DbParameter CreateDbParameter(DbCommand command, DataAccessParameter parameter) =>
            new RecordingParameter { ParameterName = parameter.ParameterName, Value = parameter.Value };

        public Task ExecuteInTransactionAsync(
            Func<IDatabaseTransaction, CancellationToken, Task> operation,
            IsolationLevel? isolationLevel = null,
            CancellationToken cancellationToken = default) =>
            ExecuteInTransactionCoreAsync(operation, isolationLevel, cancellationToken);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<IDatabaseTransaction, CancellationToken, Task<TResult>> operation,
            IsolationLevel? isolationLevel = null,
            CancellationToken cancellationToken = default) =>
            ExecuteInTransactionCoreAsync(operation, isolationLevel, cancellationToken);
    }

    private sealed class CountingResiliencePolicy : IResiliencePolicy
    {
        public int ExecutionCount { get; private set; }

        public async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return await action(cancellationToken);
        }
    }

    private sealed class RecordingConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public RecordingTransaction Transaction { get; } = new();
        public List<RecordingCommand> Commands { get; } = [];
        public int OpenCount { get; private set; }
        public bool IsDisposed { get; private set; }
        public TimeSpan CommandDelay { get; init; }
        public int MaximumConcurrentCommands { get; private set; }
        private int _activeCommands;

        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "Fake";
        public override string DataSource => "Fake";
        public override string ServerVersion => "1";
        public override ConnectionState State => _state;

        public override void Open()
        {
            OpenCount++;
            _state = ConnectionState.Open;
        }

        public override void Close() => _state = ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            Transaction.OwnerConnection = this;
            Transaction.Isolation = isolationLevel;
            return Transaction;
        }

        protected override DbCommand CreateDbCommand() => new RecordingCommand(this);

        internal async Task EnterCommandAsync(CancellationToken cancellationToken)
        {
            var active = Interlocked.Increment(ref _activeCommands);
            MaximumConcurrentCommands = Math.Max(MaximumConcurrentCommands, active);
            try
            {
                if (CommandDelay > TimeSpan.Zero)
                {
                    await Task.Delay(CommandDelay, cancellationToken);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _activeCommands);
            }
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            _state = ConnectionState.Closed;
            base.Dispose(disposing);
        }
    }

    private sealed class RecordingTransaction : DbTransaction
    {
        public RecordingConnection? OwnerConnection { get; set; }
        public IsolationLevel Isolation { get; set; }
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }
        public bool IsDisposed { get; private set; }
        public Exception? CommitException { get; set; }
        public Exception? RollbackException { get; set; }

        public override IsolationLevel IsolationLevel => Isolation;
        protected override DbConnection? DbConnection => OwnerConnection;

        public override void Commit()
        {
            CommitCount++;
            if (CommitException != null) throw CommitException;
        }

        public override void Rollback()
        {
            RollbackCount++;
            if (RollbackException != null) throw RollbackException;
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class RecordingCommand : DbCommand
    {
        private readonly RecordingConnection _connection;
        private readonly RecordingParameterCollection _parameters = new();
        private DbTransaction? _transaction;

        public RecordingCommand(RecordingConnection connection) => _connection = connection;

        public DbTransaction? AssignedTransaction => _transaction;
        [AllowNull]
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get => _connection; set { } }
        protected override DbParameterCollection DbParameterCollection => _parameters;
        protected override DbTransaction? DbTransaction { get => _transaction; set => _transaction = value; }

        public override void Cancel() { }
        public override int ExecuteNonQuery() => 1;
        public override object ExecuteScalar() => 42;
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new RecordingParameter();

        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            await _connection.EnterCommandAsync(cancellationToken);
            return 1;
        }

        public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            await _connection.EnterCommandAsync(cancellationToken);
            return 42;
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => CreateReader();

        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
            CommandBehavior behavior,
            CancellationToken cancellationToken)
        {
            await _connection.EnterCommandAsync(cancellationToken);
            return CreateReader();
        }

        private static DbDataReader CreateReader()
        {
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Rows.Add("Ada");
            return table.CreateDataReader();
        }
    }

    private sealed class RecordingParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        [AllowNull]
        public override string ParameterName { get; set; } = string.Empty;
        public override int Size { get; set; }
        [AllowNull]
        public override string SourceColumn { get; set; } = string.Empty;
        public override bool SourceColumnNullMapping { get; set; }
        public override object? Value { get; set; }
        public override void ResetDbType() { }
    }

    private sealed class RecordingParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = [];
        public override int Count => _items.Count;
        public override object SyncRoot => ((ICollection)_items).SyncRoot;
        public override int Add(object value) { _items.Add((DbParameter)value); return _items.Count - 1; }
        public override void AddRange(Array values) { foreach (var value in values) Add(value!); }
        public override void Clear() => _items.Clear();
        public override bool Contains(object value) => _items.Contains((DbParameter)value);
        public override bool Contains(string value) => _items.Any(item => item.ParameterName == value);
        public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _items.GetEnumerator();
        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _items.FindIndex(item => item.ParameterName == parameterName);
        public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _items.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName) => _items.RemoveAt(IndexOf(parameterName));
        protected override DbParameter GetParameter(int index) => _items[index];
        protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var index = IndexOf(parameterName);
            if (index < 0) _items.Add(value); else _items[index] = value;
        }
    }
}
