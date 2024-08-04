using Camille.Enums;
using Quinnlytics.Models;

namespace Quinnlytics.Services;

public interface IRiotApiService
{
    Task<string> GetCurrentGameVersionLongAsync();
    Task<string> GetCurrentGameVersionShortAsync();
    Task<Dictionary<int, Rune>> GetRunesReforgedAsync();
    Task<Match> GetMatchInformationsAsync(string matchId, Dictionary<int, Rune> runeDictionary, string playerUniqueId);
    Task<Player> GetPlayerFromApiAsync(RegionalRoute region, string gameName, string tagLine);
    Task<List<string>> GetMatchIdsByPuuidAsync(string playerUniqueId, int count = 10);
    Task FetchItemsAsync(HashSet<int> exceptions, HashSet<int> excludedItems);
    Task RefreshItemsIfVersionChangedAsync(HashSet<int> exceptions, HashSet<int> excludedItems);
    Task SavePlayerToDatabaseAsync(Player player);
    IEnumerable<Player> GetSavedPlayers();
}