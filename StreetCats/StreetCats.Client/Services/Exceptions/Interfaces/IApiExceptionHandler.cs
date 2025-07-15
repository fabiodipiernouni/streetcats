using System;
using System.Net;
using System.Threading.Tasks;
using StreetCats.Client.Models.DTOs;


namespace StreetCats.Client.Services.Exceptions.Interfaces;

/// <summary>
/// Gestione centralizzata delle eccezioni API con logging e mapping degli errori
/// Converte eccezioni HTTP in risposte user-friendly
/// </summary>
public interface IApiExceptionHandler
{
    /// <summary>
    /// Gestisce un'eccezione HTTP e restituisce una ApiResponse appropriata
    /// </summary>
    ApiResponse<T> HandleException<T>(Exception exception, string operation = "");

    /// <summary>
    /// Gestisce una HttpResponseMessage di errore
    /// </summary>
    Task<ApiResponse<T>> HandleHttpErrorAsync<T>(HttpResponseMessage response, string operation = "");

    /// <summary>
    /// Determina se un errore è retry-able
    /// </summary>
    bool IsRetryableError(Exception exception);

    /// <summary>
    /// Determina se un errore è retry-able basandosi sul status code
    /// </summary>
    bool IsRetryableStatusCode(HttpStatusCode statusCode);
}