namespace Quinnlytics.Models;

public static class RiotConstants
{
    public static readonly HashSet<int> ItemExceptions = new HashSet<int>
    {
        3006, // Berserker's Greaves
        3010 // Symbiotic Soles
    };

    public static readonly HashSet<int> ExcludedItems = new HashSet<int>
    {
        2003, // Health Potion
        2055, // Control Ward
        1102, // Gustwalker Hatchling
        1101, // Scorchclaw Pup
        1103, // Mosstomper Seedling
        3363, // Farsight Alteration
        3364, // Oracle Lens
        3340, // Stealth Ward
        2056, // Stealth Ward
        2140, // Elixir of Wrath
        2138, // Elixir of Iron
        2139, // Elixir of Sorcery
        223172, // Zephyr
        3172, // Zephyr
        3865, // World Atlas
    };
}