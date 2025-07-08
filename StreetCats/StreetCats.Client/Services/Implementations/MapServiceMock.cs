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
    // Coordinate del centro di Napoli per testing
    private readonly double DefaultLatitude = 40.8518;
    private readonly double DefaultLongitude = 14.2681;

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
            // Simula delay della geolocalizzazione
            await Task.Delay(1000);

            // Genera una posizione casuale nel centro di Napoli
            var random = new Random();
            var latVariation = (random.NextDouble() - 0.5) * 0.02; // ±1km circa
            var lngVariation = (random.NextDouble() - 0.5) * 0.02;

            return new Location
            {
                Latitude = DefaultLatitude + latVariation,
                Longitude = DefaultLongitude + lngVariation,
                Address = "Via Test, 80100 Napoli NA",
                City = "Napoli",
                PostalCode = "80100"
            };
        }
        catch (Exception)
        {
            // Se la geolocalizzazione fallisce, restituisci posizione default (centro Napoli)
            return new Location
            {
                Latitude = DefaultLatitude,
                Longitude = DefaultLongitude,
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

        // Genera indirizzo mock basato sulle coordinate
        var random = new Random((int)(latitude * longitude * 1000));
        var streets = new[]
        {
            "Via Toledo", "Spaccanapoli", "Via dei Tribunali", "Via Chiaia",
            "Corso Umberto I", "Via Scarlatti", "Via Posillipo", "Via Mergellina"
        };

        var numbers = new[] { "12", "25", "38", "47", "56", "73", "89", "94" };

        var street = streets[random.Next(streets.Length)];
        var number = numbers[random.Next(numbers.Length)];

        return $"{street}, {number} - 80100 Napoli NA";
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