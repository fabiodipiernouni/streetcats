using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StreetCats.Client.Models;

/// <summary>
/// Modello principale per rappresentare un gatto randagio
/// Usa Guid come ID per compatibilità Blazor WASM + MongoDB
/// </summary>
public class Cat
{
    /// <summary>
    /// ID unico del gatto - Guid per compatibilità MongoDB
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Nome del gatto (se conosciuto)
    /// </summary>
    [Required(ErrorMessage = "Il nome è obbligatorio")]
    [StringLength(50, ErrorMessage = "Il nome non può superare 50 caratteri")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descrizione del gatto e delle sue caratteristiche
    /// </summary>
    [StringLength(500, ErrorMessage = "La descrizione non può superare 500 caratteri")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Colore predominante del gatto
    /// </summary>
    [Required(ErrorMessage = "Il colore è obbligatorio")]
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Stato del gatto (Avvistato, Adottato, Scomparso, etc.)
    /// </summary>
    public CatStatus Status { get; set; } = CatStatus.Avvistato;

    /// <summary>
    /// URL della foto del gatto
    /// </summary>
    public string PhotoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Posizione geografica dell'avvistamento
    /// </summary>
    public Location Location { get; set; } = new();

    /// <summary>
    /// Lista dei commenti su questo gatto
    /// </summary>
    public List<Comment> Comments { get; set; } = new();

    /// <summary>
    /// Data di creazione della segnalazione
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data dell'ultimo avvistamento
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID dell'utente che ha creato la segnalazione
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Nome dell'utente che ha creato la segnalazione (per display)
    /// </summary>
    public string CreatedByName { get; set; } = string.Empty;
}

/// <summary>
/// Enumerazione per lo stato del gatto
/// </summary>
public enum CatStatus
{
    Avvistato,
    Adottato,
    Disperso,
    InCura,
    Deceduto
}