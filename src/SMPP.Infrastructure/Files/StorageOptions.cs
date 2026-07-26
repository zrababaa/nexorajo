namespace SMPP.Infrastructure.Files;

/// <summary>Bound at startup to the host's wwwroot path (SMPP.Web sets this in Program.cs).</summary>
public class StorageOptions
{
    public string RootPath { get; set; } = string.Empty;
}
