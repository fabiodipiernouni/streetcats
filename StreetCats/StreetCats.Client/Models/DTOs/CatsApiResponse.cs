using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StreetCats.Client.Models.DTOs;

/// <summary>
/// DTO per la risposta paginata dell'API dei gatti
/// </summary>
public class CatsApiResponse
{
    /// <summary>
    /// Lista dei gatti
    /// </summary>
    [JsonPropertyName("cats")]
    public List<Cat> Cats { get; set; } = new();

    /// <summary>
    /// Informazioni di paginazione
    /// </summary>
    [JsonPropertyName("pagination")]
    public PaginationInfo Pagination { get; set; } = new();
}

/// <summary>
/// Informazioni di paginazione
/// </summary>
public class PaginationInfo
{
    /// <summary>
    /// Pagina corrente
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Numero di elementi per pagina
    /// </summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 10;

    /// <summary>
    /// Numero totale di elementi
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; } = 0;

    /// <summary>
    /// Numero totale di pagine
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; } = 0;
}