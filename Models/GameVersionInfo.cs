namespace Quinnlytics.Models;

public class GameVersionInfo
{
    public string Version { get; set; }
}

public record VersionType(string Value)
{
    public string GetShortVersion()
    {
        return Value.Split('.').Take(2).Aggregate((a, b) => $"{a}.{b}");
    }
}