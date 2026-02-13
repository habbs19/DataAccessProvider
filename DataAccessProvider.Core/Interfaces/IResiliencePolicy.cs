namespace DataAccessProvider.Core.Interfaces;
/// <summary>
/// Defines a contract for executing asynchronous operations with resilience strategies, such as retries or circuit
/// breakers, to handle transient faults.
/// </summary>
/// <remarks>Implementations of this interface apply specific resilience policies to the execution of actions,
/// enabling robust handling of failures in distributed or unreliable environments. The policy determines how and when
/// to retry, fallback, or otherwise manage exceptions during the execution of the provided action.</remarks>
public interface IResiliencePolicy
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}