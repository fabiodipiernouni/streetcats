using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StreetCats.Client.Models;

/// <summary>
/// Modello per la posizione geografica (compatibile GeoJSON)
/// </summary>
public class Location
{
    /// <summary>
    /// Tipo di geometria (sempre "Point" per le posizioni)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Point";

    /// <summary>
    /// Coordinate [longitude, latitude] - formato GeoJSON
    /// </summary>
    [JsonPropertyName("coordinates")]
    public double[] Coordinates { get; set; } = new double[2];

    /// <summary>
    /// Latitudine (per facilità di accesso)
    /// </summary>
    [JsonIgnore]
    public double Latitude
    {
        get => Coordinates.Length > 1 ? Coordinates[1] : 0;
        set => Coordinates = new[] { Longitude, value };
    }

    /// <summary>
    /// Longitudine (per facilità di accesso)
    /// </summary>
    [JsonIgnore]
    public double Longitude
    {
        get => Coordinates.Length > 0 ? Coordinates[0] : 0;
        set => Coordinates = new[] { value, Latitude };
    }

    /// <summary>
    /// Indirizzo testuale della posizione
    /// </summary>
    [StringLength(200, ErrorMessage = "L'indirizzo non può superare 200 caratteri")]
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Città
    /// </summary>
    [StringLength(100, ErrorMessage = "La città non può superare 100 caratteri")]
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Codice postale
    /// </summary>
    [StringLength(10, ErrorMessage = "Il codice postale non può superare 10 caratteri")]
    [JsonPropertyName("postalCode")]
    public string PostalCode { get; set; } = string.Empty;
}