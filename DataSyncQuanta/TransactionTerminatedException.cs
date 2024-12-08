namespace DataSyncQuanta;

/// <summary>
/// Represents an exception that is thrown when a transaction is terminated to resolve a deadlock.
/// </summary>
public class TransactionTerminatedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionTerminatedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TransactionTerminatedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionTerminatedException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public TransactionTerminatedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}