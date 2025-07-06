using System;
using System.Threading.Tasks;

namespace StreetCats.Client.Services.Implementation;

/// <summary>
/// Implementazione REALE del servizio mappe (da implementare)
/// Userà API reali come Google Maps, OpenStreetMap, etc.
/// </summary>
public class MapService : IMapService
{
    private readonly HttpClient _httpClient;

    public MapService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // TODO: Implementare con API reali
    public Task<Location?> GetCurrentLocationAsync()
    {
        throw new NotImplementedException("Da implementare con API reali");
    }

    // ... altri metodi TODO
}