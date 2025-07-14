using System;
using System.Threading.Tasks;
using StreetCats.Client.Services.Interfaces;
using StreetCats.Client.Models;

namespace StreetCats.Client.Services.Implementation;

/// <summary>
/// Implementazione REALE del servizio mappe (da implementare)
/// Userà API reali
/// </summary>
public class MapService
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