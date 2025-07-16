using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StreetCats.Client.Models;

/// <summary>
/// Modello per utenti registrati su STREETCATS
/// Versione aggiornata per compatibilità con API REST e schema MongoDB
/// </summary>
public class User
{
    /// <summary>
    /// ID unico dell'utente (MongoDB _id)
    /// </summary>
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nome utente unico
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email dell'utente
    /// </summary>
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Nome completo dell'utente (OBBLIGATORIO nel backend)
    /// </summary>
    [Required]
    [StringLength(100)]
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Ruolo dell'utente (user, admin)
    /// </summary>
    [JsonPropertyName("role")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// Indica se l'utente è attivo
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Data di creazione (timestamp automatico MongoDB)
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Data di ultimo aggiornamento (timestamp automatico MongoDB)
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    // Campi extra che potrebbero essere utili lato client
    // ma non sono nel schema MongoDB base

    /// <summary>
    /// Data di ultimo accesso (campo client-side)
    /// </summary>
    [JsonIgnore]
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Avatar URL (campo client-side)
    /// </summary>
    [JsonIgnore]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Biografia dell'utente (campo client-side)
    /// </summary>
    [JsonIgnore]
    [StringLength(200)]
    public string? Bio { get; set; }

    /// <summary>
    /// Numero di gatti segnalati dall'utente (campo client-side)
    /// </summary>
    [JsonIgnore]
    public int CatsReported { get; set; } = 0;

    /// <summary>
    /// Numero di commenti scritti dall'utente (campo client-side)
    /// </summary>
    [JsonIgnore]
    public int CommentsCount { get; set; } = 0;
}

/// <summary>
/// Enumerazione per i ruoli utente
/// DEVE corrispondere esattamente ai valori nel backend: ['user', 'admin']
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    /// <summary>
    /// Utente normale
    /// </summary>
    [JsonPropertyName("user")]
    User,

    /// <summary>
    /// Amministratore (accesso completo)
    /// </summary>
    [JsonPropertyName("admin")]
    Admin
}