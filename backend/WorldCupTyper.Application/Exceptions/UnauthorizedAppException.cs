namespace WorldCupTyper.Application.Exceptions;

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message)
        : base(message, 401)
    {
    }
}
