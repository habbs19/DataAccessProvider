var transactionProvider = serviceProvider
    .GetRequiredService<IDatabaseTransactionProvider<PostgresSourceParams>>();

await transactionProvider.ExecuteInTransactionAsync(async (transaction, cancellationToken) =>
{
    await transaction.ExecuteNonQueryAsync(firstCommand, cancellationToken);
    await transaction.ExecuteNonQueryAsync(secondCommand, cancellationToken);
}, IsolationLevel.ReadCommitted, cancellationToken);

