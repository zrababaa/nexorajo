namespace AILogic.API.Configuration;

internal static class KeyFileLoader
{
    public static string? FindSiteRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        for (var level = 0; directory is not null && level < 5; level++, directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "index.html")) &&
                Directory.Exists(Path.Combine(directory.FullName, "assets")))
            {
                return directory.FullName;
            }
        }

        return null;
    }

    public static IReadOnlyDictionary<string, string?> LoadFromProjectTree(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        for (var level = 0; directory is not null && level < 5; level++, directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "key.txt");
            if (File.Exists(path))
            {
                return Parse(path);
            }
        }

        return new Dictionary<string, string?>();
    }

    private static IReadOnlyDictionary<string, string?> Parse(string path)
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var name = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"', '\'');
            if (name.Length > 0 && value.Length > 0)
            {
                settings[name] = value;
            }
        }

        return settings;
    }
}
