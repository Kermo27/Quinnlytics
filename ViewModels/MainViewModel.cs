using System.Collections.ObjectModel;
using System.Diagnostics;
using Camille.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quinnlytics.Models;
using Quinnlytics.Services;
using Quinnlytics.Views;

namespace Quinnlytics.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IRiotApiService _riotApiService;
    
    [ObservableProperty] private string _gameVersion = "???";
    
    [ObservableProperty] private ObservableCollection<PlayerViewModel> _players = new ObservableCollection<PlayerViewModel>(); 

    public MainViewModel(IRiotApiService riotApiService)
    {
        _riotApiService = riotApiService;
    }
    
    public async Task InitializeAsync()
    {
        await _riotApiService.RefreshItemsIfVersionChangedAsync(RiotConstants.ItemExceptions, RiotConstants.ExcludedItems);
        GameVersion = await _riotApiService.GetCurrentGameVersionShortAsync();

        LoadSavedPlayers();
        Debug.WriteLine($"InitializeAsync: Players.Count = {Players.Count}");
    }

    private void LoadSavedPlayers()
    {
        var savedPlayers = _riotApiService.GetSavedPlayers();
        
        Debug.WriteLine($"LoadSavedPlayers: loaded {savedPlayers.Count()} players");
        Players.Clear();
        foreach (var player in savedPlayers)
        {
            Players.Add(new PlayerViewModel(player));
        }
        Debug.WriteLine($"LoadSavedPlayers: added {Players.Count} players to collection");
        OnPropertyChanged(nameof(Players));
    }


    [RelayCommand]
    private async Task NavigateToAddPlayerPageAsync()
    {
        await Shell.Current.GoToAsync(nameof(AddPlayerPage));
    }
}