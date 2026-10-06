using System.Data;
using DataAccessProvider.Core.Interfaces;

var transactionProvider = serviceProvider
    .GetRequiredService<IDatabaseTransactionProvider<MSSQLSourceParams>>();

await transactionProvider.ExecuteInTransactionAsync(
    async (transaction, cancellationToken) =>
    {
        await transaction.ExecuteNonQueryAsync(new MSSQLSourceParams
        {
            Query = "UPDATE Accounts SET Balance = Balance - 100 WHERE Id = 1",
            CommandType = CommandType.Text
        }, cancellationToken);

        await transaction.ExecuteNonQueryAsync(new MSSQLSourceParams
        {
            Query = "UPDATE Accounts SET Balance = Balance + 100 WHERE Id = 2",
            CommandType = CommandType.Text
        }, cancellationToken);
    },
    isolationLevel: IsolationLevel.ReadCommitted,
    cancellationToken: cancellationToken);

