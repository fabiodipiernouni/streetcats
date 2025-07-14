using System.Threading;
using StreetCats.Client.Services.Configuration;
using StreetCats.Client.Services.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StreetCats.Client.Services.Interfaces;

/// <summary>
/// HttpClient personalizzato che gestisce automaticamente JWT e refresh token
/// Intercetta risposte 401 e tenta refresh automatico prima di fallire
/// </summary>
public interface IAuthenticatedHttpClient
{
    /// <summary>
    /// Esegue GET con autenticazione automatica
    /// </summary>
    Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Esegue POST con autenticazione automatica
    /// </summary>
    Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent? content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Esegue PUT con autenticazione automatica
    /// </summary>
    Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent? content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Esegue DELETE con autenticazione automatica
    /// </summary>
    Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Esegue richiesta personalizzata con autenticazione automatica
    /// </summary>
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Helper per POST con JSON
    /// </summary>
    Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Helper per PUT con JSON  
    /// </summary>
    Task<HttpResponseMessage> PutAsJsonAsync<T>(string requestUri, T value, CancellationToken cancellationToken = default);
}