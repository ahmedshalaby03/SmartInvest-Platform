namespace SmartInvest.Application.Common.Exceptions;

public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}