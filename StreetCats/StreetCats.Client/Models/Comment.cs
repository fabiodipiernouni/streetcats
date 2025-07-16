using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using StreetCats.Client.Models.DTOs;

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
    /// ID del gatto a cui appartiene il commento
    /// </summary>
    [Required]
    [JsonPropertyName("catId")]
    [JsonConverter(typeof(FlexibleGuidConverter))]
    public Guid CatId { get; set; }

    /// <summary>
    /// ID dell'utente che ha scritto il commento
    /// </summary>
    [Required]
    [JsonPropertyName("userId")]
    [JsonConverter(typeof(FlexibleGuidConverter))]
    public Guid UserId { get; set; }

    /// <summary>
    /// Nome dell'utente che ha scritto il commento (per display)
    /// </summary>
    [Required]
    [StringLength(50)]
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Testo del commento
    /// </summary>
    [Required]
    [StringLength(500, ErrorMessage = "Il commento non può superare 500 caratteri")]
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Data di creazione del commento
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data di ultima modifica del commento (se supportata)
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
    /// Indica se il commento è stato moderato/approvato
    /// </summary>
    [JsonPropertyName("isApproved")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsApproved { get; set; } = true;

    /// <summary>
    /// ID del moderatore che ha approvato il commento (se applicabile)
    /// </summary>
    [JsonPropertyName("approvedBy")]
    [JsonConverter(typeof(FlexibleNullableGuidConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ApprovedBy { get; set; }
}