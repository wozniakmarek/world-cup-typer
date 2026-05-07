namespace WorldCupTyper.Application.Exceptions;

public sealed class BusinessRuleException : AppException
{
    public BusinessRuleException(string message)
        : base(message, 400)
    {
    }
}
