using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Desktop.Services.Navigation;
using ExhibitionManagementSystem.Desktop.Services.Notifications;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Desktop.ViewModels.Base
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string? _errorMessage;

        // Pagination Properties
        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _pageSize = 20;

        [ObservableProperty]
        private int _totalCount;

        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

        protected readonly INavigationService NavigationService;
        protected readonly INotificationService NotificationService;

        protected BaseViewModel(
            INavigationService navigationService,
            INotificationService notificationService)
        {
            NavigationService = navigationService;
            NotificationService = notificationService;
        }

        public virtual Task InitializeAsync() => Task.CompletedTask;
        public virtual Task InitializeAsync(object parameter) => Task.CompletedTask;

        protected async Task<T?> ExecuteServiceAsync<T>(
            Func<Task<ServiceResult<T>>> serviceCall,
            string errorPrefix = "حدث خطأ")
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var result = await serviceCall();
                if (result.IsSuccess)
                {
                    return result.Data;
                }
                ErrorMessage = $"{errorPrefix}: {result.ErrorMessage}";
                NotificationService.ShowError(ErrorMessage);
                return default;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"{errorPrefix}: {ex.Message}";
                NotificationService.ShowError(ErrorMessage);
                return default;
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task<bool> ExecuteServiceAsync(
            Func<Task<ServiceResult>> serviceCall,
            string errorPrefix = "حدث خطأ")
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var result = await serviceCall();
                if (result.IsSuccess)
                {
                    return true;
                }
                ErrorMessage = $"{errorPrefix}: {result.ErrorMessage}";
                NotificationService.ShowError(ErrorMessage);
                return false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"{errorPrefix}: {ex.Message}";
                NotificationService.ShowError(ErrorMessage);
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}

