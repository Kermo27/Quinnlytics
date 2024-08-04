using Camille.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quinnlytics.Models;
using Quinnlytics.Services;
using Quinnlytics.Views;

namespace Quinnlytics.ViewModels;

public partial class AddPlayerViewModel : ObservableObject
{
    private readonly IRiotApiService _riotApiService;

    [ObservableProperty] private string _gameName;
    [ObservableProperty] private string _tagLine;
    
    public AddPlayerViewModel(IRiotApiService riotApiService)
    {
        _riotApiService = riotApiService;
    }

    public Task Initialize()
    {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SavePlayerAsync()
    {
        if (string.IsNullOrWhiteSpace(GameName) || string.IsNullOrWhiteSpace(TagLine))
        {
            await Shell.Current.DisplayAlert("Error", "Please enter both Game Name and Tag Line", "OK");
            return;
        }
        
        var player = await _riotApiService.GetPlayerFromApiAsync(RegionalRoute.EUROPE, GameName, TagLine);
        if (player != null)
        {
            await _riotApiService.SavePlayerToDatabaseAsync(player);
            await Shell.Current.GoToAsync("//MainPage");
        }
        else
        {
            await Shell.Current.DisplayAlert("Error", "Failed to fetch player data.", "OK");
        }
    }
}