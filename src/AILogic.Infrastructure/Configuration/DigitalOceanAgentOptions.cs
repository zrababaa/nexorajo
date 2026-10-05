namespace AILogic.Infrastructure.Configuration;

public sealed class DigitalOceanAgentOptions
{
    public string Endpoint { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
}
