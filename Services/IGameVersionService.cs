namespace Quinnlytics.Services;

public interface IGameVersionService
{
    Task SaveGameVersionAsync(string version);
    Task<string> LoadGameVersionAsync();
    Task UpdateGameVersionAsync(string newVersion);
}