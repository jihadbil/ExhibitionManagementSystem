using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Base;

public class TokenRefreshHandler : DelegatingHandler
{
    private readonly SessionService _session;
    private readonly IServiceProvider _serviceProvider;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public TokenRefreshHandler(SessionService session, IServiceProvider serviceProvider)
    {
        _session = session;
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // HttpRequestMessage (وخاصة محتواه) لا يمكن إرساله مرتين بأمان.
        // نجهز نسخة مستقلة قبل الإرسال الأول لاستخدامها عند تجديد الجلسة.
        var retryRequest = !string.IsNullOrEmpty(_session.RefreshToken)
            ? await CloneRequestAsync(request, cancellationToken)
            : null;
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && retryRequest != null)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                // Double check if token was already refreshed by another concurrent request
                var currentAccessToken = request.Headers.Authorization?.Parameter;
                if (currentAccessToken != _session.AccessToken)
                {
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
                    response.Dispose();
                    return await base.SendAsync(retryRequest, cancellationToken);
                }

                // Try to refresh token
                var refreshed = await TryRefreshTokenAsync(cancellationToken);
                if (refreshed)
                {
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
                    response.Dispose();
                    return await base.SendAsync(retryRequest, cancellationToken);
                }
                else
                {
                    // Token refresh failed or expired, log out the user
                    _session.ClearSession();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Open LoginWindow
                        var loginWindow = App.Services.GetRequiredService<Views.Auth.LoginWindow>();
                        loginWindow.Show();
                        
                        // Close all other open windows
                        var otherWindows = Application.Current.Windows.Cast<Window>()
                            .Where(w => w != loginWindow)
                            .ToList();
                        foreach (var w in otherWindows)
                        {
                            w.Close();
                        }
                    });
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        if (request.Content != null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(contentBytes);
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Use a clean HttpClient to call /api/auth/refresh-token directly, bypassing delegating handlers
            using var client = new HttpClient();
            
            var config = _serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var apiBaseUrl = config["ApiBaseUrl"]
                ?? throw new InvalidOperationException("ApiBaseUrl configuration is missing.");
            client.BaseAddress = new Uri(apiBaseUrl);
            
            var requestDto = new RefreshTokenRequestDto
            {
                AccessToken = _session.AccessToken,
                RefreshToken = _session.RefreshToken
            };

            var response = await client.PostAsJsonAsync("api/auth/refresh-token", requestDto, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponseDto>(cancellationToken: cancellationToken);
                if (result != null)
                {
                    _session.UpdateTokens(result.AccessToken, result.RefreshToken, result.ExpiresAt);
                    return true;
                }
            }
        }
        catch
        {
            // Ignore error and return false
        }
        return false;
    }
}
