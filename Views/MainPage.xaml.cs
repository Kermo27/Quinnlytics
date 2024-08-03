using Quinnlytics.ViewModels;

namespace Quinnlytics.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var viewModel = (MainViewModel)BindingContext;
        await viewModel.InitializeAsync();
    }
}