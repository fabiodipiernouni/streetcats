using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StreetCats.Client.Services.Interfaces;

public class AuthenticatedHttpClient : IAuthenticatedHttpClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly IAppSettings _appSettings;
    private readonly ILogger<AuthenticatedHttpClient>? _logger;
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);

    public AuthenticatedHttpClient(
        HttpClient httpClient,
        IAuthService authService,
        IAppSettings appSettings,
        ILogger<AuthenticatedHttpClient>? logger = null)
    {
        _httpClient = httpClient;
        _authService = authService;
        _appSettings = appSettings;
        _logger = logger;

        // Configura base address e headers di default
        ConfigureHttpClient();
    }

    #region Public API Methods

    public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        return await SendWithAuthAsync(HttpMethod.Get, requestUri, null, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent? content, CancellationToken cancellationToken = default)
    {
        return await SendWithAuthAsync(HttpMethod.Post, requestUri, content, cancellationToken);
    }

    public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent? content, CancellationToken cancellationToken = default)
    {
        return await SendWithAuthAsync(HttpMethod.Put, requestUri, content, cancellationToken);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        return await SendWithAuthAsync(HttpMethod.Delete, requestUri, null, cancellationToken);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticationAsync();
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T value, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await PostAsync(requestUri, content, cancellationToken);
    }

    public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string requestUri, T value, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await PutAsync(requestUri, content, cancellationToken);
    }

    #endregion

    #region Private Implementation

    /// <summary>
    /// Configura HttpClient con settings di base
    /// </summary>
    private void ConfigureHttpClient()
    {
        // Base address
        if (!string.IsNullOrEmpty(_appSettings.Api.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_appSettings.Api.BaseUrl);
        }

        // Timeout
        _httpClient.Timeout = _appSettings.Api.GetTimeout();

        // Headers di default
        foreach (var header in _appSettings.Api.DefaultHeaders)
        {
            if (_httpClient.DefaultRequestHeaders.Contains(header.Key))
            {
                _httpClient.DefaultRequestHeaders.Remove(header.Key);
            }
            _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        _logger?.LogDebug("🔧 HttpClient configurato: BaseUrl={BaseUrl}, Timeout={Timeout}s",
            _httpClient.BaseAddress, _appSettings.Api.TimeoutSeconds);
    }

    /// <summary>
    /// Esegue richiesta HTTP con gestione automatica dell'autenticazione
    /// </summary>
    private async Task<HttpResponseMessage> SendWithAuthAsync(
        HttpMethod method,
        string requestUri,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, requestUri) { Content = content };

        // Primo tentativo con token corrente
        await EnsureAuthenticationAsync();
        var response = await _httpClient.SendAsync(request, cancellationToken);

        // Se non è 401, ritorna la risposta
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
        {
            return response;
        }

        _logger?.LogInformation("🔄 Risposta 401 - tentativo refresh token per {Method} {Uri}", method, requestUri);

        // Tentativo refresh token (solo se abbiamo un token)
        if (_authService.IsAuthenticated && await TryRefreshTokenAsync())
        {
            _logger?.LogInformation("✅ Token refreshed - nuovo tentativo richiesta");

            // Clona e riprova la richiesta con nuovo token
            var retryRequest = await CloneRequestAsync(request);
            await EnsureAuthenticationAsync();
            response = await _httpClient.SendAsync(retryRequest, cancellationToken);
        }
        else
        {
            _logger?.LogWarning("❌ Refresh token fallito o non disponibile");
        }

        return response;
    }

    /// <summary>
    /// Assicura che le richieste abbiano il token di autenticazione
    /// </summary>
    private async Task EnsureAuthenticationAsync()
    {
        if (!_authService.IsAuthenticated)
        {
            // Non autenticato - rimuovi header Authorization se presente
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return;
        }

        var token = _authService.Token;
        if (string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return;
        }

        // Aggiungi Bearer token
        if (_httpClient.DefaultRequestHeaders.Authorization?.Parameter != token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _logger?.LogDebug("🔑 Token JWT applicato alle richieste");
        }
    }

    /// <summary>
    /// Tenta refresh del token JWT (con lock per evitare concorrenza)
    /// </summary>
    private async Task<bool> TryRefreshTokenAsync()
    {
        if (!await _refreshSemaphore.WaitAsync(TimeSpan.FromSeconds(10)))
        {
            _logger?.LogWarning("⏰ Timeout lock refresh token");
            return false;
        }

        try
        {
            // Verifica se un altro thread ha già fatto refresh
            if (_authService.IsAuthenticated)
            {
                _logger?.LogDebug("🔄 Token già refreshed da altro thread");
                return true;
            }

            // TODO: Implementare refresh token quando sarà disponibile l'endpoint
            // Per ora simuliamo un fallimento che forza re-login
            _logger?.LogInformation("🔄 Refresh token non ancora implementato - richiesto re-login");

            await _authService.LogoutAsync();
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Errore durante refresh token");

            // In caso di errore, logout per forzare re-login
            await _authService.LogoutAsync();
            return false;
        }
        finally
        {
            _refreshSemaphore.Release();
        }
    }

    /// <summary>
    /// Clona HttpRequestMessage per permettere retry
    /// </summary>
    private async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version
        };

        // Copia headers (escludi Authorization che verrà aggiunto automaticamente)
        foreach (var header in original.Headers.Where(h => h.Key != "Authorization"))
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copia properties
        foreach (var property in original.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(property.Key), property.Value);
        }

        // Copia content se presente
        if (original.Content != null)
        {
            var contentBytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            // Copia content headers
            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _refreshSemaphore?.Dispose();
        // Non disporre _httpClient perché è gestito dal DI container
    }

    #endregion
}
