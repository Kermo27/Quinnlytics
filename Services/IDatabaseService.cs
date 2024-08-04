using Quinnlytics.Models;

namespace Quinnlytics.Services;

public interface IDatabaseService
{
    Task<GameVersionInfo> GetCurrentGameVersionAsync();
    Task SaveMatchAsync(Match match);
    Task SaveNewItemsAsync(List<Item> itemsToSave);
    Task UpdateItemsAsync(Item updatedItem);
    Task<Item> GetItemByIdAsync(int id);
    Task<bool> IsMatchPresentInDatabaseAsync(string matchId);
    Task<List<RoleStat>> GetRoleStatsAsync(string gameVersion);
    Task<Dictionary<string, double>> GetRolePercentageAsync(string gameVersion);
    Task<List<Match>> GetMatchesByGameVersionAsync(string gameVersion);
    Task<string> GetItemNameAsync(int id);
    Task<Dictionary<string, Dictionary<int, string>>> GetMostPopularItemsByRoleAndSlotAsync(string gameVersion);
    List<string> ParseBuild(string buildString);
    Task<Dictionary<int, Item>> GetItemsByIdsAsync(IEnumerable<int> itemIds);
    IEnumerable<Player> GetPlayers();
    Task SavePlayerAsync(Player player);
}