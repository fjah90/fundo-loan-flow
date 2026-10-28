namespace LoanFlow.Application;

public sealed class DuplicateCustomerException(string message, Exception inner) : Exception(message, inner);
