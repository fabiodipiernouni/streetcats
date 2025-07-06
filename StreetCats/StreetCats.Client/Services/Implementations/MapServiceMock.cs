using Microsoft.JSInterop;
using StreetCats.Client.Models;
using StreetCats.Client.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace StreetCats.Client.Services.Implementation;

/// <summary>
/// Implementazione del servizio per operazioni di geolocalizzazione
/// </summary>
public class MapServiceMock : IMapService
{
    private readonly IJSRuntime _jsRuntime;

    public MapServiceMock(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Ottiene la posizione corrente usando Geolocation API del browser
    /// </summary>
    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            // Usa JavaScript Interop per chiamare Geolocation API
            var position = await _jsRuntime.InvokeAsync<GeolocationPosition>("getCurrentPosition");

            if (position?.Coords != null)
            {
                return new Location
                {
                    Latitude = position.Coords.Latitude,
                    Longitude = position.Coords.Longitude,
                    Address = await GetAddressFromCoordinatesAsync(position.Coords.Latitude, position.Coords.Longitude)
                };
            }
        }
        catch (Exception)
        {
            // Se la geolocalizzazione fallisce, restituisci posizione default (centro Napoli)
            return new Location
            {
                Latitude = 40.8518,
                Longitude = 14.2681,
                Address = "Napoli, Italia",
                City = "Napoli",
                PostalCode = "80100"
            };
        }

        return null;
    }

    /// <summary>
    /// Simulazione di geocoding inverso (in produzione useresti un servizio reale)
    /// </summary>
    public async Task<string> GetAddressFromCoordinatesAsync(double latitude, double longitude)
    {
        // Simula un delay per chiamata API
        await Task.Delay(500);

        // Mock di indirizzi basati su coordinate approssimative di Napoli
        if (IsInNaplesArea(latitude, longitude))
        {
            var addresses = new[]
            {
                "Via Chiaia, 80121 Napoli NA",
                "Corso Umberto I, 80138 Napoli NA",
                "Via del Sole, 80134 Napoli NA",
                "Piazza Garibaldi, 80142 Napoli NA",
                "Via Roma, 80132 Napoli NA",
                "Lungomare Caracciolo, 80122 Napoli NA",
                "Via Tribunali, 80138 Napoli NA",
                "Piazza del Plebiscito, 80132 Napoli NA"
            };

            // Seleziona un indirizzo basato sulle coordinate (simulazione)
            var index = Math.Abs((latitude + longitude).GetHashCode()) % addresses.Length;
            return addresses[index];
        }

        return $"Lat: {latitude:F4}, Lng: {longitude:F4}";
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

        return earthRadiusKm * c;
    }

    /// <summary>
    /// Verifica se un punto è nel raggio specificato
    /// </summary>
    public bool IsPointInRadius(double centerLat, double centerLon, double pointLat, double pointLon, double radiusKm)
    {
        var distance = CalculateDistance(centerLat, centerLon, pointLat, pointLon);
        return distance <= radiusKm;
    }

    #region Helper Methods

    private static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }

    private static bool IsInNaplesArea(double latitude, double longitude)
    {
        // Bounding box approssimativo per l'area di Napoli
        return latitude >= 40.8 && latitude <= 40.9 &&
               longitude >= 14.2 && longitude <= 14.3;
    }

    #endregion

    #region JavaScript Interop Models

    /// <summary>
    /// Modello per la risposta della Geolocation API
    /// </summary>
    public class GeolocationPosition
    {
        public GeolocationCoords? Coords { get; set; }
    }

    public class GeolocationCoords
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Accuracy { get; set; }
    }

    #endregion
}