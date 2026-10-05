namespace AILogic.Application.Exceptions;

public sealed class AgentUnavailableException : Exception
{
    public AgentUnavailableException(string message) : base(message)
    {
    }

    public AgentUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
