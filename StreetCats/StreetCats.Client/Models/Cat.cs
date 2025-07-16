using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using StreetCats.Client.Models.DTOs;

namespace StreetCats.Client.Models;

/// <summary>
/// Modello principale per rappresentare un gatto randagio
/// Usa Guid come ID per compatibilità Blazor WASM + MongoDB
/// </summary>
public class Cat
{
    /// <summary>
    /// ID unico del gatto - Guid per compatibilità MongoDB
    /// Mappa sia "_id" che "id" dalla risposta API
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleGuidConverter))]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID MongoDB originale (se diverso da Id)
    /// </summary>
    [JsonPropertyName("_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MongoId { get; set; }

    /// <summary>
    /// Nome del gatto (se conosciuto)
    /// </summary>
    [Required(ErrorMessage = "Il nome è obbligatorio")]
    [StringLength(50, ErrorMessage = "Il nome non può superare 50 caratteri")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descrizione del gatto e delle sue caratteristiche
    /// </summary>
    [StringLength(500, ErrorMessage = "La descrizione non può superare 500 caratteri")]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Colore predominante del gatto
    /// </summary>
    [Required(ErrorMessage = "Il colore è obbligatorio")]
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Stato del gatto (Avvistato, Adottato, Scomparso, etc.)
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CatStatus Status { get; set; } = CatStatus.Avvistato;

    /// <summary>
    /// URL della foto del gatto
    /// </summary>
    [JsonPropertyName("photoUrl")]
    public string PhotoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Posizione geografica dell'avvistamento
    /// </summary>
    [JsonPropertyName("location")]
    public Location Location { get; set; } = new();

    /// <summary>
    /// Lista dei commenti su questo gatto
    /// </summary>
    [JsonPropertyName("comments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Comment> Comments { get; set; } = new();

    /// <summary>
    /// Tags opzionali dal server
    /// </summary>
    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Visibilità pubblica del gatto
    /// </summary>
    [JsonPropertyName("isPublic")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Priorità del caso
    /// </summary>
    [JsonPropertyName("priority")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Priority { get; set; }

    /// <summary>
    /// Numero di segnalazioni
    /// </summary>
    [JsonPropertyName("reportCount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int ReportCount { get; set; } = 1;

    /// <summary>
    /// Data di creazione della segnalazione
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data dell'ultimo avvistamento
    /// Mappa "lastSeenAt" dalla API
    /// </summary>
    [JsonPropertyName("lastSeenAt")]
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data di ultimo aggiornamento (opzionale)
    /// </summary>
    [JsonPropertyName("updatedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Versione MongoDB (ignorata nella serializzazione)
    /// </summary>
    [JsonPropertyName("__v")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public int? Version { get; set; }

    /// <summary>
    /// ID dell'utente che ha creato la segnalazione
    /// </summary>
    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Nome dell'utente che ha creato la segnalazione (per display)
    /// </summary>
    [JsonPropertyName("createdByName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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