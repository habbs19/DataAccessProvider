using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Channels;

namespace DataAccessProvider.Core.Types;

public sealed class ConnectionPool
{
    private readonly Channel<DbConnection> _channel;
    private readonly Func<DbConnection> _connectionFactory;
    private readonly int _maxPoolSize;
    private int _createdConnections;

    public ConnectionPool(Func<DbConnection> connectionFactory, int maxPoolSize = 20)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        if (maxPoolSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPoolSize), "Max pool size must be greater than zero.");
        }

        _maxPoolSize = maxPoolSize;
        _channel = Channel.CreateBounded<DbConnection>(new BoundedChannelOptions(maxPoolSize)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        });
    }

    public async ValueTask<PooledConnectionLease> RentAsync(CancellationToken cancellationToken = default)
    {
        if (_channel.Reader.TryRead(out var existing))
        {
            return new PooledConnectionLease(existing, this);
        }

        if (TryCreateConnection(out var newConnection))
        {
            return new PooledConnectionLease(newConnection, this);
        }

        var awaited = await _channel.Reader.ReadAsync(cancellationToken);
        return new PooledConnectionLease(awaited, this);
    }

    private bool TryCreateConnection(out DbConnection connection)
    {
        while (true)
        {
            var current = Volatile.Read(ref _createdConnections);
            if (current >= _maxPoolSize)
            {
                connection = null!;
                return false;
            }

            if (Interlocked.CompareExchange(ref _createdConnections, current + 1, current) == current)
            {
                try
                {
                    connection = _connectionFactory();
                    return true;
                }
                catch
                {
                    Interlocked.Decrement(ref _createdConnections);
                    throw;
                }
            }
        }
    }

    internal void Return(DbConnection connection)
    {
        if (connection.State == ConnectionState.Broken)
        {
            DisposeConnection(connection);
            return;
        }

        if (!_channel.Writer.TryWrite(connection))
        {
            DisposeConnection(connection);
        }
    }

    private void DisposeConnection(DbConnection connection)
    {
        try
        {
            connection.Dispose();
        }
        finally
        {
            Interlocked.Decrement(ref _createdConnections);
        }
    }

    public readonly struct PooledConnectionLease : IAsyncDisposable, IDisposable
    {
        private readonly ConnectionPool _pool;

        public PooledConnectionLease(DbConnection connection, ConnectionPool pool)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }

        public DbConnection Connection { get; }

        public void Dispose()
        {
            _pool.Return(Connection);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
