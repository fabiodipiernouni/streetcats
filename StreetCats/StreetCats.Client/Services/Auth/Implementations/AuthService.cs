using Microsoft.JSInterop;
using StreetCats.Client.Models;
using StreetCats.Client.Models.DTOs;
using StreetCats.Client.Services.Http;
using StreetCats.Client.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using StreetCats.Client.Services.Auth.Interfaces;
using StreetCats.Client.Services.Configuration.Interfaces;
using StreetCats.Client.Services.Exceptions.Interfaces;

namespace StreetCats.Client.Services.Auth.Implementations;

/// <summary>
/// Implementazione REALE del servizio di autenticazione
/// Comunica con le API REST del backend per login/registrazione
/// </summary>
public class AuthService : ApiService, IAuthService
{
    private readonly IJSRuntime _jsRuntime;
    private User? _currentUser;
    private string? _token;

    // Chiavi per localStorage
    private const string TOKEN_KEY = "streetcats_token";
    private const string USER_KEY = "streetcats_user";
    private const string REFRESH_TOKEN_KEY = "streetcats_refresh_token";

    public AuthService(
        IAuthenticatedHttpClient httpClient,
        IAppSettings appSettings,
        IApiExceptionHandler exceptionHandler,
        IJSRuntime jsRuntime,
        ILogger<AuthService>? logger = null)
        : base(httpClient, appSettings, exceptionHandler, logger)
    {
        _jsRuntime = jsRuntime;
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
            LogOperationStart("InitializeAuth");

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

                            Logger?.LogInformation("✅ Sessione ripristinata per utente: {Username}", savedUser.Username);
                            AuthenticationStateChanged?.Invoke(true);
                            return;
                        }
                        else
                        {
                            Logger?.LogWarning("⚠️ Token salvato non valido - cleanup necessario");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Logger?.LogWarning(ex, "⚠️ Errore deserializzazione dati utente salvati");
                }
            }

            // Se arriviamo qui, non c'è sessione valida
            await LogoutAsync();
            Logger?.LogInformation("🔓 Nessuna sessione valida trovata");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ Errore durante inizializzazione auth");
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
            LogOperationStart("Login", new { Username = request.Username });

            // Validazione input
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ApiResponse<AuthResponse>.ErrorResponse(
                    "Username e password sono obbligatori",
                    "Credenziali mancanti"
                );
            }

            // Chiamata API login
            var endpoint = AppSettings.Api.Endpoints.AuthLogin;
            var response = await PostAsync<AuthResponse>(endpoint, request, "Login API");

            if (response.Success && response.Data != null)
            {
                // Salva stato autenticazione
                await SaveAuthStateAsync(response.Data);

                _token = response.Data.Token;
                _currentUser = response.Data.User;

                Logger?.LogInformation("✅ Login riuscito per utente: {Username}", request.Username);
                AuthenticationStateChanged?.Invoke(true);
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ Errore durante login per utente: {Username}", request.Username);
            return ExceptionHandler.HandleException<AuthResponse>(ex, "Login");
        }
    }

    /// <summary>
    /// Effettua registrazione chiamando API REST
    /// </summary>
    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            LogOperationStart("Register", new { Username = request.Username, Email = request.Email });

            // Validazioni base
            var validationError = ValidateRegisterRequest(request);
            if (validationError != null)
            {
                return validationError;
            }

            // Chiamata API registrazione
            var endpoint = AppSettings.Api.Endpoints.AuthRegister;
            var response = await PostAsync<AuthResponse>(endpoint, request, "Register API");

            if (response.Success && response.Data != null)
            {
                // Salva stato autenticazione (auto-login dopo registrazione)
                await SaveAuthStateAsync(response.Data);

                _token = response.Data.Token;
                _currentUser = response.Data.User;

                Logger?.LogInformation("✅ Registrazione riuscita per utente: {Username}", request.Username);
                AuthenticationStateChanged?.Invoke(true);
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ Errore durante registrazione per utente: {Username}", request.Username);
            return ExceptionHandler.HandleException<AuthResponse>(ex, "Register");
        }
    }

    /// <summary>
    /// Effettua logout e pulisce la sessione
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            Logger?.LogInformation("🚪 Logout utente: {Username}", _currentUser?.Username ?? "sconosciuto");

            // Pulizia stato locale
            _token = null;
            _currentUser = null;

            // Pulizia localStorage
            await RemoveFromLocalStorageAsync(TOKEN_KEY);
            await RemoveFromLocalStorageAsync(USER_KEY);
            await RemoveFromLocalStorageAsync(REFRESH_TOKEN_KEY);

            // Notifica cambiamento stato
            AuthenticationStateChanged?.Invoke(false);

            Logger?.LogInformation("✅ Logout completato");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ Errore durante logout");
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
    /// Valida un token specifico con il server
    /// </summary>
    private async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            // Semplice check: prova a ottenere il profilo utente
            var endpoint = AppSettings.Api.Endpoints.AuthProfile;

            // Temporarily set token for validation request
            var originalToken = _token;
            _token = token;

            try
            {
                var response = await GetAsync<User>(endpoint, "ValidateToken");
                return response.Success;
            }
            finally
            {
                // Restore original token
                _token = originalToken;
            }
        }
        catch (Exception ex)
        {
            Logger?.LogWarning(ex, "⚠️ Errore validazione token");
            return false;
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

            Logger?.LogDebug("💾 Stato autenticazione salvato in localStorage");
        }
        catch (Exception ex)
        {
            Logger?.LogWarning(ex, "⚠️ Errore salvataggio stato auth in localStorage");
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
            Logger?.LogWarning(ex, "⚠️ Errore salvataggio localStorage per chiave: {Key}", key);
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
            Logger?.LogWarning(ex, "⚠️ Errore lettura localStorage per chiave: {Key}", key);
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
            Logger?.LogWarning(ex, "⚠️ Errore rimozione localStorage per chiave: {Key}", key);
        }
    }

    #endregion
}