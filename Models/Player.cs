namespace Quinnlytics.Models;

public class Player
{
    public int Id { get; set; }
    public string uniquePlayerId { get; set; }
    public string encryptedSummonerId { get; set; }
    public int profileIconId { get; set; }
    public string gameName { get; set; }
    public string tagLine { get; set; }
    public long summonerLevel { get; set; }
    public string masteryTierFirst { get; set; }
    public string masteryTierSecond { get; set; }
    public string masteryTierThird { get; set; }
    public string rank { get; set; }
    public string leaguePoints { get; set; }
}