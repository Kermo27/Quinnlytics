using Newtonsoft.Json;
using Quinnlytics.Models;

namespace Quinnlytics.Services;

public class GameVersionService : IGameVersionService
{
    private const string FileName = "gameversion.json";
    
    public async Task SaveGameVersionAsync(string version)
    {
        var gameVersionInfo = new GameVersionInfo { Version = version };
        var json = JsonConvert.SerializeObject(gameVersionInfo);

        await File.WriteAllTextAsync(FileName, json);
    }

    public async Task<string> LoadGameVersionAsync()
    {
        if (File.Exists(FileName))
        {
            var json = await File.ReadAllTextAsync(FileName);
            var gameVersionInfo = JsonConvert.DeserializeObject<GameVersionInfo>(json);
            return gameVersionInfo?.Version ?? "Unknown Version";
        }

        return "Unknown Version";
    }

    public async Task UpdateGameVersionAsync(string newVersion)
    {
        var currentVersion = await LoadGameVersionAsync();
        if (currentVersion != newVersion)
        {
            await SaveGameVersionAsync(newVersion);
        }
    }
}