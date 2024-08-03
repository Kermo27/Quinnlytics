using Microsoft.EntityFrameworkCore;
using Quinnlytics.Data;
using Quinnlytics.Models;

namespace Quinnlytics.Services;

public class DatabaseService : IDatabaseService
{
    private readonly AppDbContext _context;
    
    public DatabaseService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _context.Database.EnsureCreated();
    }
    
    public async Task<GameVersion> GetCurrentGameVersionAsync()
    {
        return await _context.GameVersions.FirstOrDefaultAsync();
    }

    public async Task AddGameVersionAsync(GameVersion gameVersion)
    {
        _context.GameVersions.Add(gameVersion);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateGameVersionAsync(GameVersion gameVersion)
    {
        _context.GameVersions.Update(gameVersion);
        await _context.SaveChangesAsync();
    }

    public async Task SaveMatchAsync(Match match)
    {
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();
    }

    public async Task SaveNewItemsAsync(List<Item> itemsToSave)
    {
        var existingItemIds = _context.Items.ToList();
        var newItems = itemsToSave.Where(i => !existingItemIds.Any(e => e.Id == i.Id)).ToList();
        
        _context.Items.AddRange(newItems);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateItemsAsync(Item updatedItem)
    {
        _context.Items.Update(updatedItem);
        await _context.SaveChangesAsync();
    }

    public async Task<Item> GetItemByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Id must be a positive integer.", nameof(id));
        }

        try
        {
            return await _context.Items.FindAsync(id) ?? throw new InvalidOperationException();
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException)
        {
            throw new InvalidOperationException($"Failed to find item with ID {id}.", ex);
        }
    }

    public async Task<bool> IsMatchPresentInDatabaseAsync(string matchId)
    {
        return await _context.Matches.AnyAsync(m => m.MatchId == matchId);
    }

    public async Task<List<RoleStat>> GetRoleStatsAsync(string gameVersion)
    {
        var roleStatsQuery = _context.Matches
            .Where(match => match.GameVersion.StartsWith(gameVersion))
            .GroupBy(match => match.Role)
            .Select(group => new RoleStat
            {
                Role = group.Key,
                GameCount = group.Count(),
                WinRatio = group.Average(match => match.Win ? 1 : 0),
                KDA = (double)group.Sum(match => match.Kills + match.Assists) /
                      (group.Sum(match => match.Deaths) == 0 ? 1 : group.Sum(match => match.Deaths)),
                MostFrequentOpponent = group.GroupBy(match => match.Opponent)
                    .OrderByDescending(opponentGroup => opponentGroup.Count())
                    .Select(opponentGroup => opponentGroup.Key)
                    .FirstOrDefault() ?? "Unknown",
                AverageGameDuration = TimeSpan.FromSeconds(group.Average(match => match.GameDuration))
                    .ToString(@"hh\:mm\:ss"),
                MinionsPerMinute = group.Average(match => match.MinionsPerMinutes)
            });

        var roleStats = await roleStatsQuery.ToListAsync();

        foreach (var roleStat in roleStats.Where(roleStat => roleStat.Role == "UTILITY"))
        {
            roleStat.Role = "SUPPORT";
        }

        return roleStats;
    }

    public async Task<Dictionary<string, double>> GetRolePercentageAsync(string gameVersion)
    {
        var matches = await GetMatchesByGameVersionAsync(gameVersion);
        var totalMatchCount = matches.Count;

        var rolePercentages = matches
            .GroupBy(match => match.Role)
            .ToDictionary(group => group.Key, group => (double)group.Count() / totalMatchCount);

        return rolePercentages;
    }

    public async Task<List<Match>> GetMatchesByGameVersionAsync(string gameVersion)
    {
        return await _context.Matches
            .Where(match => match.GameVersion == gameVersion)
            .ToListAsync();
    }

    public async Task<string> GetItemNameAsync(int id)
    {
        var item = await _context.Items.FindAsync(id);
        return item?.Name ?? "Unknown Item";
    }

    public async Task<Dictionary<string, Dictionary<int, string>>> GetMostPopularItemsByRoleAndSlotAsync(string gameVersion)
    {
        var matches = await GetMatchesByGameVersionAsync(gameVersion);
        var roleBuilds = matches.GroupBy(match => match.Role)
            .ToDictionary(group => group.Key, group => group.Select(match => match.Build).ToList());
        
        var mostPopularItems = new Dictionary<string, Dictionary<int, string>>();

        foreach (var roleBuild in roleBuilds)
        {
            var role = roleBuild.Key;
            var builds = roleBuild.Value;

            var slotItemCounts = Enumerable.Range(1,6).ToDictionary(slot => slot, _ => new Dictionary<string, int>());

            foreach (var build in builds)
            {
                var items = ParseBuild(build);

                for (int slot = 1; slot < items.Count; slot++)
                {
                    var item = items[slot - 1];

                    if (slot > 6)
                    {
                        Console.WriteLine($"Invalid slot detected: {slot}. Build: {build}");
                        continue;
                    }

                    if (!slotItemCounts[slot].ContainsKey(item))
                    {
                        slotItemCounts[slot][item] = 0;
                    }

                    slotItemCounts[slot][item]++;
                }
            }
            
            var rolePopularItems = new Dictionary<int, string>();

            foreach (var slot in slotItemCounts.Keys)
            {
                var itemCounts = slotItemCounts[slot];
                var mostPopularItem = itemCounts.OrderByDescending(x => x.Value).FirstOrDefault().Key;
                rolePopularItems[slot] = mostPopularItem ?? "Yuumi";
            }
            
            mostPopularItems[role] = rolePopularItems;
        }

        return mostPopularItems;
    }

    public List<string> ParseBuild(string buildString)
    {
        var items = buildString.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList();
        return items;
    }
    
    public async Task<Dictionary<int, Item>> GetItemsByIdsAsync(IEnumerable<int> itemIds)
    {
        return await _context.Items
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id);
    }
}