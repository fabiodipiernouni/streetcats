using System;
using System.ComponentModel.DataAnnotations;

namespace StreetCats.Client.Models;

/// <summary>
/// Modello per gli utenti dell'applicazione
/// </summary>
public class User
{
    /// <summary>
    /// ID unico dell'utente
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Nome utente (username)
    /// </summary>
    [Required(ErrorMessage = "Il nome utente è obbligatorio")]
    [StringLength(50, ErrorMessage = "Il nome utente non può superare 50 caratteri")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email dell'utente
    /// </summary>
    [Required(ErrorMessage = "L'email è obbligatoria")]
    [EmailAddress(ErrorMessage = "Formato email non valido")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Nome completo dell'utente
    /// </summary>
    [StringLength(100, ErrorMessage = "Il nome completo non può superare 100 caratteri")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Data di registrazione
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indica se l'utente è attivo
    /// </summary>
    public bool IsActive { get; set; } = true;
}