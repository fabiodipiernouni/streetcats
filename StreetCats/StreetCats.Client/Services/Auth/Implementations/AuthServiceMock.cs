using Microsoft.JSInterop;
using StreetCats.Client.Models;
using StreetCats.Client.Models.DTOs;
using StreetCats.Client.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using StreetCats.Client.Services.Auth.Interfaces;

namespace StreetCats.Client.Services.Auth.Implementations;

/// <summary>
/// Implementazione mock del servizio di autenticazione
/// </summary>
public class AuthServiceMock : IAuthService
{
    private readonly IJSRuntime _jsRuntime;
    private User? _currentUser;
    private string? _token;

    public AuthServiceMock(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public User? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null && !string.IsNullOrEmpty(_token);
    public string? Token => _token;

    public event Action<bool>? AuthenticationStateChanged;

    /// <summary>
    /// Inizializza il servizio controllando se esiste un token salvato
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var savedToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "streetcats_token");
            var savedUser = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "streetcats_user");

            if (!string.IsNullOrEmpty(savedToken) && !string.IsNullOrEmpty(savedUser))
            {
                _token = savedToken;
                _currentUser = JsonSerializer.Deserialize<User>(savedUser);

                // Verifica se il token è ancora valido (mock - sempre true per semplicità)
                if (await ValidateTokenAsync())
                {
                    AuthenticationStateChanged?.Invoke(true);
                    return;
                }
            }
        }
        catch (Exception)
        {
            // Se c'è un errore nel caricamento, pulisci lo stato
            await LogoutAsync();
        }
    }

    /// <summary>
    /// Mock del login - accetta qualsiasi username/password per demo
    /// </summary>
    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        // Simula delay di rete
        await Task.Delay(1000);

        // Mock - accetta qualsiasi login per demo
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "Email e password sono obbligatori",
                Errors = new List<string> { "Credenziali mancanti" }
            };
        }

        // Simula utenti di test
        var mockUser = CreateMockUser(request.Email);
        var mockToken = GenerateMockToken(mockUser);

        var authResponse = new AuthResponse
        {
            Token = mockToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = mockUser
        };

        // Salva in localStorage
        await SaveAuthStateAsync(mockToken, mockUser);

        _token = mockToken;
        _currentUser = mockUser;

        AuthenticationStateChanged?.Invoke(true);

        return new ApiResponse<AuthResponse>
        {
            Success = true,
            Message = "Login effettuato con successo",
            Data = authResponse
        };
    }

    /// <summary>
    /// Mock della registrazione
    /// </summary>
    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        // Simula delay di rete
        await Task.Delay(1500);

        // Validazioni mock
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
        {
            return new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "Il nome utente deve avere almeno 3 caratteri",
                Errors = new List<string> { "Nome utente non valido" }
            };
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "La password deve avere almeno 6 caratteri",
                Errors = new List<string> { "Password non valida" }
            };
        }

        if (request.Password != request.ConfirmPassword)
        {
            return new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "Le password non coincidono",
                Errors = new List<string> { "Conferma password non valida" }
            };
        }

        // Simula controllo username esistente (mock - alcuni username sono "già presi")
        var existingUsers = new[] { "admin", "test", "demo" };
        if (existingUsers.Contains(request.Username.ToLower()))
        {
            return new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "Nome utente già in uso",
                Errors = new List<string> { "Username non disponibile" }
            };
        }

        // Crea nuovo utente
        var newUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var mockToken = GenerateMockToken(newUser);

        var authResponse = new AuthResponse
        {
            Token = mockToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = newUser
        };

        // Salva stato
        await SaveAuthStateAsync(mockToken, newUser);

        _token = mockToken;
        _currentUser = newUser;

        AuthenticationStateChanged?.Invoke(true);

        return new ApiResponse<AuthResponse>
        {
            Success = true,
            Message = "Registrazione completata con successo",
            Data = authResponse
        };
    }

    /// <summary>
    /// Logout dell'utente
    /// </summary>
    public async Task LogoutAsync()
    {
        _token = null;
        _currentUser = null;

        // Pulisci localStorage
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "streetcats_token");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "streetcats_user");

        AuthenticationStateChanged?.Invoke(false);
    }

    /// <summary>
    /// Validazione del token (mock - sempre valido)
    /// </summary>
    public async Task<bool> ValidateTokenAsync()
    {
        // Simula chiamata API per validazione
        await Task.Delay(200);

        // Mock - considera valido se esiste
        return !string.IsNullOrEmpty(_token) && _currentUser != null;
    }

    #region Helper Methods

    private User CreateMockUser(string username)
    {
        return new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = username,
            Email = $"{username}@example.com",
            FullName = $"Utente {username}",
            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 365)),
            IsActive = true
        };
    }

    private string GenerateMockToken(User user)
    {
        // Mock token - in produzione sarebbe un JWT reale
        var payload = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new
        {
            sub = user.Id.ToString(),
            username = user.Username,
            exp = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds()
        }));

        return $"mock.{payload}.signature";
    }

    private async Task SaveAuthStateAsync(string token, User user)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "streetcats_token", token);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "streetcats_user", JsonSerializer.Serialize(user));
    }

    #endregion
}