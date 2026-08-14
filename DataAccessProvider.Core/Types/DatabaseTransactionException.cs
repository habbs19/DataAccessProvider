namespace DataAccessProvider.Core.Types;

public enum DatabaseTransactionFailureStage
{
    Commit,
    Rollback
}

/// <summary>
/// Represents a transaction failure that needs more context than the provider
/// exception alone can supply.
/// </summary>
public sealed class DatabaseTransactionException : Exception
{
    internal DatabaseTransactionException(
        string message,
        DatabaseTransactionFailureStage failureStage,
        Exception primaryException,
        Exception? rollbackException,
        bool commitOutcomeUnknown)
        : base(message, primaryException)
    {
        FailureStage = failureStage;
        PrimaryException = primaryException;
        RollbackException = rollbackException;
        CommitOutcomeUnknown = commitOutcomeUnknown;
    }

    public DatabaseTransactionFailureStage FailureStage { get; }

    public Exception PrimaryException { get; }

    public Exception? RollbackException { get; }

    /// <summary>
    /// Gets whether the server may have committed even though the client received an error.
    /// </summary>
    public bool CommitOutcomeUnknown { get; }
}
