using Camille.Enums;
using Camille.RiotGames;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quinnlytics.Models;

namespace Quinnlytics.Services;

public class RiotApiService : IRiotApiService
{
    private const RegionalRoute Region = RegionalRoute.EUROPE;
    private readonly RiotGamesApi _riotGamesApi;
    private readonly HttpClient _httpClient;
    private readonly IDatabaseService _databaseService;
    private readonly IGameVersionService _gameVersionService;
    private readonly string _apiKey;
    private string? _currentGameVersion;

    public RiotApiService(IDatabaseService databaseService, IHttpClientFactory httpClientFactory, IGameVersionService gameVersionService)
    {
        _apiKey = "RGAPI-24bffdb3-2ad7-4c70-8b38-e2e2c2ac44d2";
        _riotGamesApi = RiotGamesApi.NewInstance(_apiKey);
        _databaseService = databaseService ?? throw new ArgumentException(nameof(databaseService));
        _httpClient = httpClientFactory.CreateClient("RiotApiClient");
        _gameVersionService = gameVersionService;
    }

    public async Task<string> GetCurrentGameVersionLongAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("api/versions.json");
            var versions = JsonConvert.DeserializeObject<string[]>(response);

            if (versions == null || versions.Length == 0)
            {
                throw new Exception("No versions found");
            }

            return versions[0];
        }
        catch (Exception ex)
        {
            throw new Exception($"An error occurred: {ex.Message}");
        }
    }

    public async Task<string> GetCurrentGameVersionShortAsync()
    {
        if (_currentGameVersion == null)
        {
            _currentGameVersion = await GetCurrentGameVersionLongAsync();
        }
        
        var shortVersion = string.Join(".", _currentGameVersion.Split('.').Take(2));
        
        return shortVersion;
    }

    public async Task<Dictionary<int, Rune>> GetRunesReforgedAsync()
    {
        try
        {
            if (_currentGameVersion == null)
            {
                _currentGameVersion = await GetCurrentGameVersionLongAsync();
            }
            
            var response = await _httpClient.GetStringAsync($"cdn/{_currentGameVersion}/data/en_US/runesReforged.json");
            var runes = JsonConvert.DeserializeObject<List<Rune>>(response);

            if (runes == null || runes.Count == 0)
            {
                throw new Exception("No runes found");
            }

            return runes.SelectMany(r => r.Slots.SelectMany(s => s.Runes)).ToDictionary(r => r.Id);
        }
        catch (Exception ex)
        {
            throw new Exception($"An error occurred: {ex.Message}");
        }
    }

    public async Task<Match> GetMatchInformationsAsync(
        string matchId,
        Dictionary<int, Rune> runeDictionary,
        string playerUniqueId
    )
    {
        var matchTask = _riotGamesApi.MatchV5().GetMatchAsync(Region, matchId);
        var timelineTask = _riotGamesApi.MatchV5().GetTimelineAsync(Region, matchId);

        await Task.WhenAll(matchTask, timelineTask);

        var match = matchTask.Result;
        var timeline = timelineTask.Result;

        var participant = match.Info.Participants.FirstOrDefault(p => p.Puuid == playerUniqueId);
        if (participant == null)
        {
            throw new ArgumentException("Player not found in the match.");
        }

        var participantId = participant.ParticipantId;

        var purchases = timeline
            .Info.Frames.SelectMany(frame => frame.Events)
            .Where(e => e.Type == "ITEM_PURCHASED" && e.ParticipantId == participantId)
            .OrderBy(e => e.Timestamp)
            .Select(e => e.ItemId)
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .ToList();

        var player = match.Info.Participants.FirstOrDefault(p => p.Puuid == playerUniqueId);
        if (player == null)
        {
            throw new ArgumentException("Player not found in the match.");
        }

        var endGameItemsId = new List<int>
        {
            player.Item0,
            player.Item1,
            player.Item2,
            player.Item3,
            player.Item4,
            player.Item5
        };

        var itemIds = purchases.Concat(endGameItemsId).Distinct().ToList();
        var items = await _databaseService.GetItemsByIdsAsync(itemIds);

        var build = purchases
            .Where(endGameItemsId.Contains)
            .Where(items.ContainsKey)
            .Select(itemId => items[itemId].Name)
            .Take(6)
            .ToList();

        var opponent =
            match
                .Info.Participants.FirstOrDefault(op =>
                    op.TeamPosition == player.TeamPosition && op.TeamId != player.TeamId
                )
                ?.ChampionName ?? "Unknown";

        var runeDetails = player
            .Perks.Styles.SelectMany(style => style.Selections)
            .Select(selection =>
                runeDictionary.TryGetValue(selection.Perk, out var rune)
                    ? rune.Name
                    : $"Rune ID: {selection.Perk}"
            )
            .ToArray();

        var matchEntity = new Match
        {
            MatchDate = match.Info.GameCreation,
            PlayerUniqueId = playerUniqueId,
            MatchId = matchId,
            Role = player.TeamPosition == "UTILITY" ? "SUPPORT" : player.TeamPosition,
            Win = player.Win,
            Opponent = opponent,
            SummonerSpells = $"Summoner1: {player.Summoner1Id}, Summoner2: {player.Summoner2Id}",
            Champion = player.ChampionName,
            GameVersion = string.Join(".", match.Info.GameVersion.Split('.').Take(2)),
            GameDuration = match.Info.GameDuration,
            RuneDetails = string.Join(", ", runeDetails),
            Kills = player.Kills,
            Deaths = player.Deaths,
            Assists = player.Assists,
            TotalMinionsKilled = player.TotalMinionsKilled + player.NeutralMinionsKilled,
            MinionsPerMinutes =
                (player.TotalMinionsKilled + player.NeutralMinionsKilled)
                / (match.Info.GameDuration / 60f),
            QSkillUsage = player.Spell1Casts,
            WSkillUsage = player.Spell2Casts,
            ESkillUsage = player.Spell3Casts,
            RSkillUsage = player.Spell4Casts,
            AllInPings = player.AllInPings,
            AssistMePings = player.AssistMePings,
            CommandPings = player.CommandPings,
            EnemyMissingPings = player.EnemyMissingPings,
            EnemyVisionPings = player.EnemyVisionPings,
            GetBackPings = player.GetBackPings,
            NeedVisionPings = player.NeedVisionPings,
            OnMyWayPings = player.OnMyWayPings,
            PushPings = player.PushPings,
            GoldEarned = player.GoldEarned,
            GoldSpent = player.GoldSpent,
            Build = string.Join(", ", build)
        };

        return matchEntity;
    }

    public async Task<Player> GetPlayerFromApiAsync(RegionalRoute region, string gameName, string tagLine)
    {
        var summoner = await _riotGamesApi.AccountV1().GetByRiotIdAsync(region, gameName, tagLine);
        if (summoner == null)
        {
            throw new ArgumentException("Summoner not found");
        }
        var player = new Player
        {
            GameName = gameName,
            TagLine = tagLine,
            UniquePlayerId = summoner.Puuid,
            RegionPlayer = "EUW"
        };
        
        return player;
    }

    public async Task SavePlayerToDatabaseAsync(Player player)
    {
        await _databaseService.SavePlayerAsync(player);
    }

    public IEnumerable<Player> GetSavedPlayers()
    {
        return _databaseService.GetPlayers();
    }

    public async Task<List<string>> GetMatchIdsByPuuidAsync(string playerUniqueId, int count = 10)
    {
        var matchIds = await _riotGamesApi
            .MatchV5()
            .GetMatchIdsByPUUIDAsync(Region, playerUniqueId, count: count);
        var draftMatchIds = new List<string>();

        foreach (var matchId in matchIds)
        {
            var match = await _riotGamesApi.MatchV5().GetMatchAsync(Region, matchId);
            if (
                (int)match.Info.QueueId == 400
                || (int)match.Info.QueueId == 420
                || (int)match.Info.QueueId == 440
            )
            {
                draftMatchIds.Add(matchId);
            }
        }

        return draftMatchIds;
    }

    public async Task FetchItemsAsync(HashSet<int> excludedItems, HashSet<int> exceptions)
    {
        if (_currentGameVersion == null)
        {
            _currentGameVersion = await GetCurrentGameVersionLongAsync();
        }
        
        var response = await _httpClient.GetStringAsync($"cdn/{_currentGameVersion}/data/en_US/item.json");
        
        var itemData = JsonConvert.DeserializeObject<JObject>(response);

        var itemsToSave = new List<Item>();

        foreach (var item in itemData["data"].Children<JProperty>())
        {
            var itemId = int.Parse(item.Name);
            var itemDetails = item.Value;
            var itemName = (string)itemDetails["name"];
            var intoArray = itemDetails["into"] as JArray;

            var hasIntoItems = intoArray != null && intoArray.Count > 0;
            var isExcluded = excludedItems.Contains(itemId);
            var isException = exceptions.Contains(itemId);

            if ((!hasIntoItems || isException) && !isExcluded)
            {
                var existingItem = await _databaseService.GetItemByIdAsync(itemId);
                if (existingItem != null)
                {
                    existingItem.Id = itemId;
                    existingItem.Name = itemName;
                    await _databaseService.UpdateItemsAsync(existingItem);
                }
                else
                {
                    itemsToSave.Add(new Item { Id = itemId, Name = itemName });
                }
            }
        }

        if (itemsToSave.Count != 0)
        {
            await _databaseService.SaveNewItemsAsync(itemsToSave);
        }
    }

    public async Task RefreshItemsIfVersionChangedAsync(
        HashSet<int> excludedItems,
        HashSet<int> exceptions
    )
    {
        _currentGameVersion = await GetCurrentGameVersionLongAsync();

        var gameVersionInFile = await _gameVersionService.LoadGameVersionAsync();

        if (gameVersionInFile != _currentGameVersion)
        {
            await _gameVersionService.UpdateGameVersionAsync(_currentGameVersion);
            
            await FetchItemsAsync(excludedItems, exceptions);
        }
    }
}
