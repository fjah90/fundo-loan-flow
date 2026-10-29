namespace LoanFlow.Application;

public sealed class CustomerConflictException(string message, Exception inner) : Exception(message, inner);
