using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Quinnlytics.Models;

namespace Quinnlytics.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    [ObservableProperty]
    private string _gameName;

    [ObservableProperty]
    private string _tagLine;

    public PlayerViewModel(Player player)
    {
        GameName = player.GameName;
        TagLine = player.TagLine;
        Debug.WriteLine($"Created PlayerViewModel: {GameName}, {TagLine}");
    }
}