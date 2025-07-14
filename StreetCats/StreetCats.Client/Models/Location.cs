using System.ComponentModel.DataAnnotations;

namespace StreetCats.Client.Models;

/// <summary>
/// Modello per la posizione geografica (compatibile GeoJSON)
/// </summary>
public class Location
{
    /// <summary>
    /// Tipo di geometria (sempre "Point" per le posizioni)
    /// </summary>
    public string Type { get; set; } = "Point";

    /// <summary>
    /// Coordinate [longitude, latitude] - formato GeoJSON
    /// </summary>
    public double[] Coordinates { get; set; } = new double[2];

    /// <summary>
    /// Latitudine (per facilità di accesso)
    /// </summary>
    public double Latitude
    {
        get => Coordinates.Length > 1 ? Coordinates[1] : 0;
        set => Coordinates = new[] { Longitude, value };
    }

    /// <summary>
    /// Longitudine (per facilità di accesso)
    /// </summary>
    public double Longitude
    {
        get => Coordinates.Length > 0 ? Coordinates[0] : 0;
        set => Coordinates = new[] { value, Latitude };
    }

    /// <summary>
    /// Indirizzo testuale della posizione
    /// </summary>
    [StringLength(200, ErrorMessage = "L'indirizzo non può superare 200 caratteri")]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Città
    /// </summary>
    [StringLength(100, ErrorMessage = "La città non può superare 100 caratteri")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Codice postale
    /// </summary>
    [StringLength(10, ErrorMessage = "Il codice postale non può superare 10 caratteri")]
    public string PostalCode { get; set; } = string.Empty;
}