using System;

using System.Collections.Generic;

/// <summary>
/// Response per autenticazione con token JWT
/// Versione aggiornata con informazioni estese
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Token JWT per autenticazione
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token per rinnovare l'accesso
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Data/ora di scadenza del token
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Dati dell'utente autenticato
    /// </summary>
    public User User { get; set; } = new();

    /// <summary>
    /// Tipo di token (es. "Bearer")
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Scope/permessi del token
    /// </summary>
    public List<string> Scopes { get; set; } = new();

    /// <summary>
    /// Verifica se il token è ancora valido
    /// </summary>
    public bool IsTokenValid => DateTime.UtcNow < ExpiresAt;

    /// <summary>
    /// Minuti rimanenti prima della scadenza
    /// </summary>
    public int MinutesUntilExpiry => IsTokenValid ?
        (int)(ExpiresAt - DateTime.UtcNow).TotalMinutes : 0;

    /// <summary>
    /// Verifica se il token scade presto (entro i prossimi 5 minuti)
    /// </summary>
    public bool IsExpiringSoon => MinutesUntilExpiry <= 5;
}
