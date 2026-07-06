namespace MeetingAgent.Infrastructure.Configuration;

public static class DotEnv
{
    public static void Load(string filePath = ".env")
    {
        var resolvedPath = ResolvePath(filePath);
        if (resolvedPath is null) return;

        foreach (var rawLine in File.ReadAllLines(resolvedPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            var separatorIndex = line.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0) continue;

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(key)) continue;
            if (Environment.GetEnvironmentVariable(key) is not null) continue;

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string? ResolvePath(string filePath)
    {
        if (Path.IsPathRooted(filePath))
        {
            return File.Exists(filePath) ? filePath : null;
        }

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, filePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
