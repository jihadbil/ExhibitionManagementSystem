using System;
using System.Windows.Controls;

namespace ExhibitionManagementSystem.DeskTop.Services.Navigation;

public interface INavigationService
{
    /// <summary>
    /// المسار الحالي (مثال: "Dashboard", "Exhibitions")
    /// </summary>
    string CurrentRoute { get; }

    /// <summary>
    /// ينقل إلى صفحة محددة بنوعها
    /// </summary>
    void NavigateTo<TPage>() where TPage : UserControl;

    /// <summary>
    /// يُسجّل الـ Frame الرئيسي من MainShellWindow
    /// </summary>
    void SetFrame(Frame frame);

    /// <summary>
    /// يُطلَق بعد كل عملية تنقل — الـ SidebarControl يستمع له
    /// </summary>
    event EventHandler<string> Navigated;
}
