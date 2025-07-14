
namespace StreetCats.Client.Models.Api;

/// <summary>
/// Configurazione per API esterne (Google Maps, geocoding, ecc.)
/// </summary>
public class ExternalApisConfiguration
{
    /// <summary>
    /// Google Maps API Key per geocoding
    /// </summary>
    public string? GoogleMapsApiKey { get; set; }

    /// <summary>
    /// URL base per OpenStreetMap Nominatim (geocoding gratuito)
    /// </summary>
    public string NominatimBaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    /// <summary>
    /// Rate limit per Nominatim (richieste per secondo)
    /// </summary>
    public double NominatimRateLimit { get; set; } = 1.0;
}