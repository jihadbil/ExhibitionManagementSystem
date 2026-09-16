using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Base;

public abstract class ApiClientBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SessionService _session;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    protected ApiClientBase(IHttpClientFactory httpClientFactory, SessionService session)
    {
        _httpClientFactory = httpClientFactory;
        _session = session;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("ApiClient");
        if (!string.IsNullOrEmpty(_session.AccessToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
        }
        return client;
    }

    protected async Task<ServiceResult<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync(endpoint);
            return await HandleResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            return ServiceResult<T>.Failure(ex.Message);
        }
    }

    protected async Task<ServiceResult<T>> PostAsync<T>(string endpoint, object body)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync(endpoint, body);
            return await HandleResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            return ServiceResult<T>.Failure(ex.Message);
        }
    }

    protected async Task<ServiceResult> PostVoidAsync(string endpoint, object body)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync(endpoint, body);
            return await HandleResponseAsync(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }

    protected async Task<ServiceResult<T>> PutAsync<T>(string endpoint, object body)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PutAsJsonAsync(endpoint, body);
            return await HandleResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            return ServiceResult<T>.Failure(ex.Message);
        }
    }

    protected async Task<ServiceResult> PutVoidAsync(string endpoint, object body)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PutAsJsonAsync(endpoint, body);
            return await HandleResponseAsync(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }

    protected async Task<ServiceResult<T>> PatchAsync<T>(string endpoint, object? body = null)
    {
        try
        {
            var client = CreateClient();
            HttpResponseMessage response;
            if (body != null)
            {
                response = await client.PatchAsJsonAsync(endpoint, body);
            }
            else
            {
                response = await client.PatchAsync(endpoint, null);
            }
            return await HandleResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            return ServiceResult<T>.Failure(ex.Message);
        }
    }

    protected async Task<ServiceResult> PatchVoidAsync(string endpoint, object? body = null)
    {
        try
        {
            var client = CreateClient();
            HttpResponseMessage response;
            if (body != null)
            {
                response = await client.PatchAsJsonAsync(endpoint, body);
            }
            else
            {
                response = await client.PatchAsync(endpoint, null);
            }
            return await HandleResponseAsync(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }

    protected async Task<ServiceResult> DeleteAsync(string endpoint)
    {
        try
        {
            var client = CreateClient();
            var response = await client.DeleteAsync(endpoint);
            return await HandleResponseAsync(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }

    private async Task<ServiceResult<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return ServiceResult<T>.Success(default!);
            }
            var data = await response.Content.ReadFromJsonAsync<T>(_jsonSerializerOptions);
            return ServiceResult<T>.Success(data!);
        }

        var errorResponse = await TryReadErrorAsync(response);
        return ServiceResult<T>.Failure(errorResponse.Message, errorResponse.Code);
    }

    private async Task<ServiceResult> HandleResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return ServiceResult.Success();
        }

        var errorResponse = await TryReadErrorAsync(response);
        return ServiceResult.Failure(errorResponse.Message, errorResponse.Code);
    }

    private async Task<(string Message, string? Code)> TryReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(content))
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                
                string? message = null;
                string? errorCode = null;

                if (root.TryGetProperty("errorMessage", out var msgProp) || root.TryGetProperty("ErrorMessage", out msgProp))
                {
                    message = msgProp.GetString();
                }
                else if (root.TryGetProperty("message", out var mProp) || root.TryGetProperty("Message", out mProp))
                {
                    message = mProp.GetString();
                }

                if (root.TryGetProperty("errorCode", out var codeProp) || root.TryGetProperty("ErrorCode", out codeProp))
                {
                    errorCode = codeProp.GetString();
                }

                if (!string.IsNullOrEmpty(message))
                {
                    return (message, errorCode);
                }
            }
        }
        catch
        {
            // Fallback if JSON parsing fails
        }

        return (response.ReasonPhrase ?? $"HTTP {(int)response.StatusCode}", response.StatusCode.ToString());
    }
}
