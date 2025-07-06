using System;
using System.ComponentModel.DataAnnotations;

namespace StreetCats.Client.Models;

/// <summary>
/// Modello per i commenti sui gatti
/// </summary>
public class Comment
{
    /// <summary>
    /// ID unico del commento
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID del gatto a cui si riferisce il commento
    /// </summary>
    public Guid CatId { get; set; }

    /// <summary>
    /// ID dell'utente che ha scritto il commento
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Nome dell'utente (per display)
    /// </summary>
    [Required(ErrorMessage = "Il nome utente è obbligatorio")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Testo del commento
    /// </summary>
    [Required(ErrorMessage = "Il testo del commento è obbligatorio")]
    [StringLength(1000, ErrorMessage = "Il commento non può superare 1000 caratteri")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Data di creazione del commento
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}