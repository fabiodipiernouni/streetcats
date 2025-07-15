using Microsoft.JSInterop;
using StreetCats.Client.Models;
using StreetCats.Client.Services.Interfaces;
using System.Text.Json;
using StreetCats.Client.Services.Configuration.Interfaces;

namespace StreetCats.Client.Services.Implementations;

/// <summary>
/// Implementazione REALE del servizio per operazioni di geolocalizzazione e mappe
/// Usa Nominatim (OpenStreetMap) per geocoding gratuito - ideale per progetti universitari
/// </summary>
public class MapService : IMapService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly IAppSettings _appSettings;
    private readonly ILogger<MapService>? _logger;

    // Rate limiting per Nominatim (max 1 richiesta al secondo)
    private DateTime _lastNominatimRequest = DateTime.MinValue;
    private readonly TimeSpan _nominatimDelay = TimeSpan.FromSeconds(1);

    public MapService(
        HttpClient httpClient,
        IJSRuntime jsRuntime,
        IAppSettings appSettings,
        ILogger<MapService>? logger = null)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _appSettings = appSettings;
        _logger = logger;

        // Configura HttpClient per Nominatim
        ConfigureHttpClient();
    }

    #region Public API Methods

    /// <summary>
    /// Ottiene la posizione corrente usando Geolocation API del browser
    /// </summary>
    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            _logger?.LogDebug("📍 Richiesta posizione corrente...");

            // Usa JavaScript Interop per geolocalizzazione
            var position = await _jsRuntime.InvokeAsync<GeolocationResult>("StreetCatsInterop.getCurrentLocation");

            if (position != null)
            {
                var location = new Location
                {
                    Latitude = position.Latitude,
                    Longitude = position.Longitude,
                    Address = "", // Sarà popolato con geocoding inverso se necessario
                    City = "",
                    PostalCode = ""
                };

                _logger?.LogInformation("✅ Posizione ottenuta: {Lat}, {Lng} (precisione: {Accuracy}m)",
                    position.Latitude, position.Longitude, position.Accuracy);

                return location;
            }

            _logger?.LogWarning("⚠️ Geolocalizzazione non disponibile");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Errore durante geolocalizzazione");

            // Fallback: restituisci posizione default (centro Napoli)
            return GetDefaultNaplesLocation();
        }
    }

    /// <summary>
    /// Converte coordinate in indirizzo usando Nominatim (geocoding inverso)
    /// </summary>
    public async Task<string> GetAddressFromCoordinatesAsync(double latitude, double longitude)
    {
        try
        {
            _logger?.LogDebug("🔍 Geocoding inverso per: {Lat}, {Lng}", latitude, longitude);

            // Rispetta rate limit di Nominatim
            await EnsureNominatimRateLimit();

            var url = BuildNominatimReverseUrl(latitude, longitude);
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var nominatimResponse = JsonSerializer.Deserialize<NominatimReverseResponse>(jsonContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (nominatimResponse != null && !string.IsNullOrEmpty(nominatimResponse.DisplayName))
                {
                    var address = FormatNominatimAddress(nominatimResponse);
                    _logger?.LogInformation("✅ Indirizzo trovato: {Address}", address);
                    return address;
                }
            }

            _logger?.LogWarning("⚠️ Geocoding inverso fallito per: {Lat}, {Lng}", latitude, longitude);
            return $"Posizione: {latitude:F6}, {longitude:F6}";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Errore geocoding inverso per: {Lat}, {Lng}", latitude, longitude);
            return $"Posizione: {latitude:F6}, {longitude:F6}";
        }
    }

    /// <summary>
    /// Calcola distanza tra due punti usando formula Haversine
    /// </summary>
    public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        var distance = earthRadiusKm * c;

        _logger?.LogDebug("📏 Distanza calcolata: {Distance:F2} km tra ({Lat1}, {Lon1}) e ({Lat2}, {Lon2})",
            distance, lat1, lon1, lat2, lon2);

        return distance;
    }

    /// <summary>
    /// Verifica se un punto è entro un raggio specifico
    /// </summary>
    public bool IsPointInRadius(double centerLat, double centerLon, double pointLat, double pointLon, double radiusKm)
    {
        var distance = CalculateDistance(centerLat, centerLon, pointLat, pointLon);
        var isInRadius = distance <= radiusKm;

        _logger?.LogDebug("🎯 Punto ({PointLat}, {PointLon}) è {Status} dal centro ({CenterLat}, {CenterLon}) con raggio {Radius}km (distanza: {Distance:F2}km)",
            pointLat, pointLon, isInRadius ? "DENTRO" : "FUORI", centerLat, centerLon, radiusKm, distance);

        return isInRadius;
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Configura HttpClient per chiamate Nominatim
    /// </summary>
    private void ConfigureHttpClient()
    {
        // User-Agent obbligatorio per Nominatim
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "StreetCats-University-Project/1.0 (student@example.com)");
        _httpClient.Timeout = TimeSpan.FromSeconds(10); // Timeout ragionevole per geocoding
    }

    /// <summary>
    /// Costruisce URL per geocoding inverso Nominatim
    /// </summary>
    private string BuildNominatimReverseUrl(double latitude, double longitude)
    {
        var baseUrl = _appSettings.Api.ExternalApis.NominatimBaseUrl.TrimEnd('/');
        return $"{baseUrl}/reverse?format=json&lat={latitude:F6}&lon={longitude:F6}&addressdetails=1&extratags=1";
    }

    /// <summary>
    /// Rispetta rate limit di Nominatim (1 richiesta al secondo)
    /// </summary>
    private async Task EnsureNominatimRateLimit()
    {
        var timeSinceLastRequest = DateTime.UtcNow - _lastNominatimRequest;

        if (timeSinceLastRequest < _nominatimDelay)
        {
            var waitTime = _nominatimDelay - timeSinceLastRequest;
            _logger?.LogDebug("⏳ Rate limit Nominatim: aspetto {WaitMs}ms", waitTime.TotalMilliseconds);
            await Task.Delay(waitTime);
        }

        _lastNominatimRequest = DateTime.UtcNow;
    }

    /// <summary>
    /// Formatta risposta Nominatim in indirizzo leggibile
    /// </summary>
    private string FormatNominatimAddress(NominatimReverseResponse response)
    {
        try
        {
            var address = response.Address;
            if (address == null) return response.DisplayName ?? "";

            // Costruisci indirizzo in formato italiano
            var parts = new List<string>();

            // Via/strada
            if (!string.IsNullOrEmpty(address.Road))
            {
                var streetPart = address.Road;
                if (!string.IsNullOrEmpty(address.HouseNumber))
                {
                    streetPart += $", {address.HouseNumber}";
                }
                parts.Add(streetPart);
            }

            // Zona/quartiere
            if (!string.IsNullOrEmpty(address.Suburb))
            {
                parts.Add(address.Suburb);
            }
            else if (!string.IsNullOrEmpty(address.Neighbourhood))
            {
                parts.Add(address.Neighbourhood);
            }

            // Città
            if (!string.IsNullOrEmpty(address.City))
            {
                parts.Add(address.City);
            }
            else if (!string.IsNullOrEmpty(address.Town))
            {
                parts.Add(address.Town);
            }
            else if (!string.IsNullOrEmpty(address.Village))
            {
                parts.Add(address.Village);
            }

            // CAP se disponibile
            if (!string.IsNullOrEmpty(address.Postcode))
            {
                parts.Add(address.Postcode);
            }

            return parts.Any() ? string.Join(", ", parts) : response.DisplayName ?? "";
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "⚠️ Errore formattazione indirizzo Nominatim");
            return response.DisplayName ?? "";
        }
    }

    /// <summary>
    /// Converte gradi in radianti
    /// </summary>
    private static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }

    /// <summary>
    /// Restituisce posizione default (centro Napoli) in caso di errore
    /// </summary>
    private Location GetDefaultNaplesLocation()
    {
        return new Location
        {
            Latitude = 40.8518,
            Longitude = 14.2681,
            Address = "Napoli, Italia",
            City = "Napoli",
            PostalCode = "80100"
        };
    }

    #endregion

    #region Data Models for JavaScript Interop and Nominatim

    /// <summary>
    /// Modello per risultato geolocalizzazione JavaScript
    /// </summary>
    public class GeolocationResult
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Accuracy { get; set; }
        public string Timestamp { get; set; } = "";
    }

    /// <summary>
    /// Modello per risposta Nominatim reverse geocoding
    /// </summary>
    public class NominatimReverseResponse
    {
        public string? DisplayName { get; set; }
        public NominatimAddress? Address { get; set; }
    }

    /// <summary>
    /// Modello per indirizzo Nominatim
    /// </summary>
    public class NominatimAddress
    {
        public string? Road { get; set; }
        public string? HouseNumber { get; set; }
        public string? Suburb { get; set; }
        public string? Neighbourhood { get; set; }
        public string? City { get; set; }
        public string? Town { get; set; }
        public string? Village { get; set; }
        public string? Postcode { get; set; }
        public string? Country { get; set; }
        public string? CountryCode { get; set; }
    }

    #endregion
}