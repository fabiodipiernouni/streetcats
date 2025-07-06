using StreetCats.Client.Models;
using StreetCats.Client.Models.DTOs;
using System;
using System.Threading.Tasks;

namespace StreetCats.Client.Services.Interfaces;

/// <summary>
/// Servizio per la gestione dell'autenticazione e autorizzazione
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Utente attualmente autenticato (null se non autenticato)
    /// </summary>
    User? CurrentUser { get; }

    /// <summary>
    /// Indica se l'utente è attualmente autenticato
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Token JWT corrente
    /// </summary>
    string? Token { get; }

    /// <summary>
    /// Evento scatenato quando lo stato di autenticazione cambia
    /// </summary>
    event Action<bool>? AuthenticationStateChanged;

    /// <summary>
    /// Effettua il login dell'utente
    /// </summary>
    /// <param name="request">Dati di login</param>
    /// <returns>Risultato dell'operazione</returns>
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);

    /// <summary>
    /// Registra un nuovo utente
    /// </summary>
    /// <param name="request">Dati di registrazione</param>
    /// <returns>Risultato dell'operazione</returns>
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Effettua il logout dell'utente
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Inizializza il servizio (controlla token salvato)
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Verifica se il token è valido e non scaduto
    /// </summary>
    Task<bool> ValidateTokenAsync();
}