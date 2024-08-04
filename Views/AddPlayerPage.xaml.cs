using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quinnlytics.ViewModels;

namespace Quinnlytics.Views;

public partial class AddPlayerPage : ContentPage
{
    public AddPlayerPage(AddPlayerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var viewModel = (AddPlayerViewModel)BindingContext;
        await viewModel.Initialize();
    }
}