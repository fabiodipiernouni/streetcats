using System;
using System.ComponentModel.DataAnnotations;

namespace StreetCats.Client.Models;

/// <summary>
/// Modello per utenti registrati su STREETCATS
/// Versione aggiornata per compatibilità con API REST
/// </summary>
public class User
{
    /// <summary>
    /// ID unico dell'utente
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Nome utente unico
    /// </summary>
    [Required]
    [StringLength(30, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email dell'utente
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Nome completo dell'utente (opzionale)
    /// </summary>
    [StringLength(100)]
    public string? FullName { get; set; }

    /// <summary>
    /// Data di registrazione
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data di ultimo accesso
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Indica se l'utente è attivo
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indica se l'email è stata verificata
    /// </summary>
    public bool IsEmailVerified { get; set; } = false;

    /// <summary>
    /// Avatar URL (opzionale)
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Biografia dell'utente (opzionale)
    /// </summary>
    [StringLength(200)]
    public string? Bio { get; set; }

    /// <summary>
    /// Ruolo dell'utente (User, Moderator, Admin)
    /// </summary>
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// Numero di gatti segnalati dall'utente
    /// </summary>
    public int CatsReported { get; set; } = 0;

    /// <summary>
    /// Numero di commenti scritti dall'utente
    /// </summary>
    public int CommentsCount { get; set; } = 0;
}

/// <summary>
/// Enumerazione per i ruoli utente
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Utente normale
    /// </summary>
    User,

    /// <summary>
    /// Moderatore (può moderare contenuti)
    /// </summary>
    Moderator,

    /// <summary>
    /// Amministratore (accesso completo)
    /// </summary>
    Admin
}