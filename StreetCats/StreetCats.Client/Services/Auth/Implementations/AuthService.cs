using Microsoft.JSInterop;
using StreetCats.Client.Models;
using StreetCats.Client.Models.DTOs;
using StreetCats.Client.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using StreetCats.Client.Services.Auth.Interfaces;
using StreetCats.Client.Services.Configuration.Interfaces;
using StreetCats.Client.Services.Exceptions.Interfaces;
using System.Net.Http;
using System.Text;

namespace StreetCats.Client.Services.Auth.Implementations;

/// <summary>
/// Implementazione REALE del servizio di autenticazione
/// Comunica con le API REST del backend per login/registrazione
/// USA HttpClient NORMALE per evitare dipendenza circolare
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IAppSettings _appSettings;
    private readonly IApiExceptionHandler _exceptionHandler;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<AuthService>? _logger;

    private User? _currentUser;
    private string? _token;

    // Chiavi per localStorage
    private const string TOKEN_KEY = "streetcats_token";
    private const string USER_KEY = "streetcats_user";
    private const string REFRESH_TOKEN_KEY = "streetcats_refresh_token";

    public AuthService(
        HttpClient httpClient,  // HttpClient normale, NON IAuthenticatedHttpClient
        IAppSettings appSettings,
        IApiExceptionHandler exceptionHandler,
        IJSRuntime jsRuntime,
        ILogger<AuthService>? logger = null)
    {
        _httpClient = httpClient;
        _appSettings = appSettings;
        _exceptionHandler = exceptionHandler;
        _jsRuntime = jsRuntime;
        _logger = logger;

        // Configura HttpClient per API
        ConfigureHttpClient();
    }

    #region Public Properties

    public User? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null && !string.IsNullOrEmpty(_token);
    public string? Token => _token;

    public event Action<bool>? AuthenticationStateChanged;

    #endregion

    #region Public Methods

    /// <summary>
    /// Inizializza il servizio controllando se esiste un token salvato
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger?.LogInformation("Inizializzazione AuthService...");

            // Carica token e utente dal localStorage
            var savedToken = await GetFromLocalStorageAsync<string>(TOKEN_KEY);
            var savedUserJson = await GetFromLocalStorageAsync<string>(USER_KEY);

            if (!string.IsNullOrEmpty(savedToken) && !string.IsNullOrEmpty(savedUserJson))
            {
                try
                {
                    var savedUser = JsonSerializer.Deserialize<User>(savedUserJson, GetJsonOptions());

                    if (savedUser != null)
                    {
                        // Valida il token con il server
                        if (await ValidateTokenAsync(savedToken))
                        {
                            _token = savedToken;
                            _currentUser = savedUser;

                            _logger?.LogInformation("Sessione ripristinata per utente: {Username}", savedUser.Username);
                            AuthenticationStateChanged?.Invoke(true);
                            return;
                        }
                        else
                        {
                            _logger?.LogWarning("Token salvato non valido - cleanup necessario");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger?.LogWarning(ex, "Errore deserializzazione dati utente salvati");
                }
            }

            // Se arriviamo qui, non c'è sessione valida
            await LogoutAsync();
            _logger?.LogInformation("Nessuna sessione valida trovata");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Errore durante inizializzazione auth");
            await LogoutAsync(); // Cleanup in caso di errore
        }
    }

    /// <summary>
    /// Effettua login chiamando API REST
    /// </summary>
    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            _logger?.LogInformation("Tentativo login per utente: {Username}", request.Username);

            // Validazione input
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ApiResponse<AuthResponse>.ErrorResponse(
                    "Username e password sono obbligatori",
                    "Credenziali mancanti"
                );
            }

            // Chiamata API login
            var endpoint = $"{_appSettings.Api.BaseUrl}{_appSettings.Api.Endpoints.AuthLogin}";
            var response = await PostAsync<AuthResponse>(endpoint, request);

            if (response.Success && response.Data != null)
            {
                // Salva stato autenticazione
                await SaveAuthStateAsync(response.Data);

                _token = response.Data.Token;
                _currentUser = response.Data.User;

                _logger?.LogInformation("Login riuscito per utente: {Username}", request.Username);
                AuthenticationStateChanged?.Invoke(true);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Errore durante login per utente: {Username}", request.Username);
            return _exceptionHandler.HandleException<AuthResponse>(ex, "Login");
        }
    }

    /// <summary>
    /// Effettua registrazione chiamando API REST
    /// </summary>
    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            _logger?.LogInformation("Tentativo registrazione per utente: {Username}, Email: {Email}",
                request.Username, request.Email);

            // Validazioni base
            var validationError = ValidateRegisterRequest(request);
            if (validationError != null)
            {
                return validationError;
            }

            // Chiamata API registrazione
            var endpoint = $"{_appSettings.Api.BaseUrl}{_appSettings.Api.Endpoints.AuthRegister}";
            var response = await PostAsync<AuthResponse>(endpoint, request);

            if (response.Success && response.Data != null)
            {
                // Salva stato autenticazione (auto-login dopo registrazione)
                await SaveAuthStateAsync(response.Data);

                _token = response.Data.Token;
                _currentUser = response.Data.User;

                _logger?.LogInformation("Registrazione riuscita per utente: {Username}", request.Username);
                AuthenticationStateChanged?.Invoke(true);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Errore durante registrazione per utente: {Username}", request.Username);
            return _exceptionHandler.HandleException<AuthResponse>(ex, "Register");
        }
    }

    /// <summary>
    /// Effettua logout e pulisce la sessione
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            _logger?.LogInformation("Logout utente: {Username}", _currentUser?.Username ?? "sconosciuto");

            // Pulizia stato locale
            _token = null;
            _currentUser = null;

            // Pulizia localStorage
            await RemoveFromLocalStorageAsync(TOKEN_KEY);
            await RemoveFromLocalStorageAsync(USER_KEY);
            await RemoveFromLocalStorageAsync(REFRESH_TOKEN_KEY);

            // Notifica cambiamento stato
            AuthenticationStateChanged?.Invoke(false);

            _logger?.LogInformation("Logout completato");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Errore durante logout");
        }
    }

    /// <summary>
    /// Valida il token corrente con il server
    /// </summary>
    public async Task<bool> ValidateTokenAsync()
    {
        if (string.IsNullOrEmpty(_token))
        {
            return false;
        }

        return await ValidateTokenAsync(_token);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Configura HttpClient per chiamate API
    /// </summary>
    private void ConfigureHttpClient()
    {
        // Headers di default
        foreach (var header in _appSettings.Api.DefaultHeaders)
        {
            if (!_httpClient.DefaultRequestHeaders.Contains(header.Key))
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        // Timeout
        _httpClient.Timeout = _appSettings.Api.GetTimeout();
    }

    /// <summary>
    /// Valida un token specifico con il server
    /// </summary>
    private async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var endpoint = $"{_appSettings.Api.BaseUrl}{_appSettings.Api.Endpoints.AuthProfile}";

            // Crea richiesta con token
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Errore validazione token");
            return false;
        }
    }

    /// <summary>
    /// Effettua chiamata POST generica
    /// </summary>
    private async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, GetJsonOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(responseContent, GetJsonOptions());
                return result ?? ApiResponse<T>.ErrorResponse("Risposta vuota dal server");
            }
            else
            {
                // Prova a deserializzare errore strutturato
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ApiResponse<T>>(responseContent, GetJsonOptions());
                    return errorResponse ?? ApiResponse<T>.ErrorResponse($"Errore HTTP {response.StatusCode}");
                }
                catch
                {
                    return ApiResponse<T>.ErrorResponse($"Errore HTTP {response.StatusCode}: {responseContent}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Errore chiamata POST a {Endpoint}", endpoint);
            return ApiResponse<T>.ErrorResponse($"Errore di rete: {ex.Message}");
        }
    }

    /// <summary>
    /// Salva stato autenticazione in localStorage
    /// </summary>
    private async Task SaveAuthStateAsync(AuthResponse authResponse)
    {
        try
        {
            await SaveToLocalStorageAsync(TOKEN_KEY, authResponse.Token);
            await SaveToLocalStorageAsync(USER_KEY, JsonSerializer.Serialize(authResponse.User, GetJsonOptions()));

            if (!string.IsNullOrEmpty(authResponse.RefreshToken))
            {
                await SaveToLocalStorageAsync(REFRESH_TOKEN_KEY, authResponse.RefreshToken);
            }

            _logger?.LogDebug("Stato autenticazione salvato in localStorage");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Errore salvataggio stato auth in localStorage");
        }
    }

    /// <summary>
    /// Valida richiesta di registrazione
    /// </summary>
    private ApiResponse<AuthResponse>? ValidateRegisterRequest(RegisterRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
        {
            errors.Add("Il nome utente deve avere almeno 3 caratteri");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
        {
            errors.Add("Email non valida");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            errors.Add("La password deve avere almeno 6 caratteri");
        }

        if (request.Password != request.ConfirmPassword)
        {
            errors.Add("Le password non coincidono");
        }

        if (errors.Any())
        {
            return ApiResponse<AuthResponse>.ErrorResponse(
                "Dati di registrazione non validi",
                errors
            );
        }

        return null;
    }

    /// <summary>
    /// Validazione email semplice
    /// </summary>
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Opzioni JSON standardizzate
    /// </summary>
    private JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #endregion

    #region LocalStorage Helpers

    /// <summary>
    /// Salva valore in localStorage
    /// </summary>
    private async Task SaveToLocalStorageAsync<T>(string key, T value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value?.ToString());
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Errore salvataggio localStorage per chiave: {Key}", key);
        }
    }

    /// <summary>
    /// Recupera valore da localStorage
    /// </summary>
    private async Task<T?> GetFromLocalStorageAsync<T>(string key)
    {
        try
        {
            var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);

            if (string.IsNullOrEmpty(value))
            {
                return default;
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)value;
            }

            return JsonSerializer.Deserialize<T>(value, GetJsonOptions());
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Errore lettura localStorage per chiave: {Key}", key);
            return default;
        }
    }

    /// <summary>
    /// Rimuove valore da localStorage
    /// </summary>
    private async Task RemoveFromLocalStorageAsync(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Errore rimozione localStorage per chiave: {Key}", key);
        }
    }

    #endregion
}