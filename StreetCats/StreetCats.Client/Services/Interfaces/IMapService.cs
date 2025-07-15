using StreetCats.Client.Models;
using System.Threading.Tasks;

namespace StreetCats.Client.Services.Interfaces;



/// <summary>
/// Servizio per operazioni di geolocalizzazione e mappe
/// </summary>
public interface IMapService
{
    /// <summary>
    /// Ottiene la posizione corrente dell'utente
    /// </summary>
    /// <returns>Coordinate attuali o null se non disponibili</returns>
    Task<Location?> GetCurrentLocationAsync();

    /// <summary>
    /// Converte coordinate in indirizzo testuale (geocoding inverso)
    /// </summary>
    /// <param name="latitude">Latitudine</param>
    /// <param name="longitude">Longitudine</param>
    /// <returns>Indirizzo formattato</returns>
    Task<string> GetAddressFromCoordinatesAsync(double latitude, double longitude);

    /// <summary>
    /// Calcola la distanza tra due punti in chilometri
    /// </summary>
    /// <param name="lat1">Latitudine punto 1</param>
    /// <param name="lon1">Longitudine punto 1</param>
    /// <param name="lat2">Latitudine punto 2</param>
    /// <param name="lon2">Longitudine punto 2</param>
    /// <returns>Distanza in chilometri</returns>
    double CalculateDistance(double lat1, double lon1, double lat2, double lon2);

    /// <summary>
    /// Verifica se un punto è entro un raggio specifico
    /// </summary>
    /// <param name="centerLat">Latitudine centro</param>
    /// <param name="centerLon">Longitudine centro</param>
    /// <param name="pointLat">Latitudine punto</param>
    /// <param name="pointLon">Longitudine punto</param>
    /// <param name="radiusKm">Raggio in chilometri</param>
    /// <returns>True se il punto è nel raggio</returns>
    bool IsPointInRadius(double centerLat, double centerLon, double pointLat, double pointLon, double radiusKm);
}