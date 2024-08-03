using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quinnlytics.Services;
using Quinnlytics.Views;

namespace Quinnlytics.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IRiotApiService _riotApiService;
    
    [ObservableProperty] private string _gameVersion = "???";
    
    // Informations about player
    [ObservableProperty] private string _playerNick = "Kermo#Asuna";
    [ObservableProperty] private string _playerLevel = "69";
    [ObservableProperty] private string _playerRank = "Diamond III";
    [ObservableProperty] private string _playerPoints = "67";
    [ObservableProperty] private string _secondPlayerNick = "Asuna#Kermo";
    [ObservableProperty] private string _secondPlayerLevel = "96";
    [ObservableProperty] private string _secondPlayerRank = "Platinum III";
    [ObservableProperty] private string _secondPlayerPoints = "12";

    public MainViewModel(IRiotApiService riotApiService)
    {
        _riotApiService = riotApiService;
    }
    
    public async Task InitializeAsync()
    {
        GameVersion = await _riotApiService.GetCurrentGameVersionShortAsync();
        PlayerNick = "Кермо#AIBOT";
        PlayerLevel = "Level 69";
        PlayerRank = "Diamond III";
        PlayerPoints = "67 LP";
        SecondPlayerNick = "Asuna#Kermo";
        SecondPlayerLevel = "Level 96";
        SecondPlayerRank = "Platinum III";
        SecondPlayerPoints = "12 LP";
    }

    [RelayCommand]
    private async Task ConfigButtonClicked()
    {
        await Shell.Current.GoToAsync(nameof(ConfigurationPage));
    }
}