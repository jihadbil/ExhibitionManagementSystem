using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace ExhibitionManagementSystem.DeskTop.Services.Navigation;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private Frame? _frame;

    public string CurrentRoute { get; private set; } = string.Empty;
    public event EventHandler<string>? Navigated;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetFrame(Frame frame)
    {
        _frame = frame;
    }

    public void NavigateTo<TPage>() where TPage : UserControl
    {
        if (_frame == null)
            throw new InvalidOperationException("Frame لم يُسجَّل بعد. استدعِ SetFrame أولاً.");

        // إنشاء صفحة جديدة من DI
        var page = _serviceProvider.GetRequiredService<TPage>();

        Application.Current.Dispatcher.Invoke(() =>
        {
            _frame.Navigate(page);
            CurrentRoute = typeof(TPage).Name.Replace("Page", "").Replace("Window", "");
            Navigated?.Invoke(this, CurrentRoute);
        });
    }
}
