using System;
using System.ComponentModel.DataAnnotations;

namespace StreetCats.Client.Models;

/// <summary>
/// Modello per commenti sui gatti
/// Versione aggiornata per compatibilità con API REST
/// </summary>
public class Comment
{
    /// <summary>
    /// ID unico del commento
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID del gatto a cui appartiene il commento
    /// </summary>
    [Required]
    public Guid CatId { get; set; }

    /// <summary>
    /// ID dell'utente che ha scritto il commento
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Nome dell'utente che ha scritto il commento (per display)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Testo del commento
    /// </summary>
    [Required]
    [StringLength(500, ErrorMessage = "Il commento non può superare 500 caratteri")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Data di creazione del commento
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data di ultima modifica del commento (se supportata)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Indica se il commento è stato moderato/approvato
    /// </summary>
    public bool IsApproved { get; set; } = true;

    /// <summary>
    /// ID del moderatore che ha approvato il commento (se applicabile)
    /// </summary>
    public Guid? ApprovedBy { get; set; }
}