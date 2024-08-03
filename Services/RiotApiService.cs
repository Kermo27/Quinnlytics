﻿using Camille.Enums;
using Camille.RiotGames;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quinnlytics.Models;

namespace Quinnlytics.Services;

public class RiotApiService : IRiotApiService
{
    private const RegionalRoute Region = RegionalRoute.EUROPE;
    private readonly RiotGamesApi _riotGamesApi;
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly DatabaseService _databaseService;
    private readonly string _apiKey;
    private string? _currentGameVersionLong;

    public RiotApiService(DatabaseService databaseService)
    {
        _apiKey = "RGAPI-24bffdb3-2ad7-4c70-8b38-e2e2c2ac44d2";
        _riotGamesApi = RiotGamesApi.NewInstance(_apiKey);
        _databaseService = databaseService ?? throw new ArgumentException(nameof(databaseService));
    }

    public async Task InitializeAsync()
    {
        _currentGameVersionLong = await GetCurrentGameVersionLongAsync();
    }

    public async Task<string> GetCurrentGameVersionLongAsync()
    {
        try
        {
            var response = await _httpClient
                .GetStringAsync("https://ddragon.leagueoflegends.com/api/versions.json")
                .ConfigureAwait(false);
            var versions = JsonConvert.DeserializeObject<string[]>(response);

            if (versions == null || versions.Length == 0)
            {
                throw new Exception("No versions found");
            }

            return versions[0];
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"HTTP request failed: {ex.Message}");
        }
        catch (JsonReaderException ex)
        {
            throw new Exception($"JSON deserialization failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new Exception($"An error occurred: {ex.Message}");
        }
    }

    public async Task<string> GetCurrentGameVersionShortAsync()
    {
        try
        {
            var response = await _httpClient
                .GetStringAsync("https://ddragon.leagueoflegends.com/api/versions.json")
                .ConfigureAwait(false);
            var versions = JsonConvert.DeserializeObject<string[]>(response);

            if (versions == null || versions.Length == 0)
            {
                throw new Exception("No versions found");
            }

            var shortVersion = string.Join(".", versions[0].Split('.').Take(2));
            return shortVersion;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"HTTP request failed: {ex.Message}");
        }
        catch (JsonReaderException ex)
        {
            throw new Exception($"JSON deserialization failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new Exception($"An error occurred: {ex.Message}");
        }
    }

    public async Task<Dictionary<int, Rune>> GetRunesReforgedAsync()
    {
        try
        {
            var response = await _httpClient
                .GetStringAsync(
                    $"https://ddragon.leagueoflegends.com/cdn/{_currentGameVersionLong}/data/en_US/runesReforged.json"
                )
                .ConfigureAwait(false);
            var runes = JsonConvert.DeserializeObject<List<Rune>>(response);

            if (runes == null || runes.Count == 0)
            {
                throw new Exception("No runes found");
            }

            return runes.SelectMany(r => r.Slots.SelectMany(s => s.Runes)).ToDictionary(r => r.Id);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"HTTP request failed: {ex.Message}");
        }
        catch (JsonReaderException ex)
        {
            throw new Exception($"JSON deserialization failed: {ex.Message}");
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

        var endGameItems = new List<int>
        {
            player.Item0,
            player.Item1,
            player.Item2,
            player.Item3,
            player.Item4,
            player.Item5
        };

        var itemIds = purchases.Concat(endGameItems).Distinct().ToList();
        var items = await _databaseService.GetItemsByIdsAsync(itemIds);

        var build = purchases
            .Where(endGameItems.Contains)
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

    public async Task<string> GetSummonerPuuidAsync(string gameName, string tagLine)
    {
        var summoner = await _riotGamesApi.AccountV1().GetByRiotIdAsync(Region, gameName, tagLine);
        return summoner.Puuid;
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
        var response = await _httpClient
            .GetStringAsync(
                $"https://ddragon.leagueoflegends.com/cdn/{_currentGameVersionLong}/data/en_US/item.json"
            )
            .ConfigureAwait(false);
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

        if (itemsToSave.Any())
        {
            await _databaseService.SaveNewItemsAsync(itemsToSave);
        }
    }

    public async Task RefreshItemsIfVersionChangedAsync(
        HashSet<int> excludedItems,
        HashSet<int> exceptions
    )
    {
        var currentVersion = await GetCurrentGameVersionLongAsync();
        var gameVersionInDatabase = await _databaseService.GetCurrentGameVersionAsync();

        if (gameVersionInDatabase == null || gameVersionInDatabase.Version != currentVersion)
        {
            if (gameVersionInDatabase == null)
            {
                gameVersionInDatabase = new GameVersion { Version = currentVersion };
                await _databaseService.AddGameVersionAsync(gameVersionInDatabase);
            }
            else
            {
                gameVersionInDatabase.Version = currentVersion;
                await _databaseService.UpdateGameVersionAsync(gameVersionInDatabase);
            }
            
            await FetchItemsAsync(excludedItems, exceptions);
        }
    }
}
