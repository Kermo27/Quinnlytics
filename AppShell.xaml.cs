﻿using Quinnlytics.Views;

namespace Quinnlytics;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(AddPlayerPage), typeof(AddPlayerPage));
    }
}