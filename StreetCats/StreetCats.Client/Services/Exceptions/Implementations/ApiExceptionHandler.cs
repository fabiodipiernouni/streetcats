using StreetCats.Client.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using StreetCats.Client.Services.Exceptions.Interfaces;

namespace StreetCats.Client.Services.Exceptions.Implementations;

public class ApiExceptionHandler : IApiExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler>? _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gestisce eccezioni generiche e le converte in ApiResponse
    /// </summary>
    public async Task<ApiResponse<T>> HandleExceptionAsync<T>(Exception exception, string operation = "")
    {
        var context = string.IsNullOrEmpty(operation) ? "Operazione API" : operation;

        // Log dell'errore
        _logger?.LogError(exception, "❌ Errore durante {Operation}: {Message}", context, exception.Message);

        return exception switch
        {
            HttpRequestException httpEx => await HandleHttpRequestExceptionAsync<T>(httpEx, context),
            TaskCanceledException timeoutEx => HandleTimeoutException<T>(timeoutEx, context),
            JsonException jsonEx => HandleJsonException<T>(jsonEx, context),
            UnauthorizedAccessException authEx => HandleAuthException<T>(authEx, context),
            ApiException apiEx => HandleApiException<T>(apiEx, context),
            _ => HandleGenericException<T>(exception, context)
        };
    }

    /// <summary>
    /// Gestisce HttpResponseMessage di errore e converte in ApiResponse
    /// </summary>
    public async Task<ApiResponse<T>> HandleHttpErrorAsync<T>(HttpResponseMessage response, string operation = "")
    {
        var context = string.IsNullOrEmpty(operation) ? "Richiesta HTTP" : operation;

        try
        {
            var content = await response.Content.ReadAsStringAsync();

            _logger?.LogWarning("⚠️ HTTP {StatusCode} durante {Operation}: {Content}",
                response.StatusCode, context, content);

            // Prova a deserializzare una risposta di errore strutturata
            if (!string.IsNullOrWhiteSpace(content))
            {
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (errorResponse != null)
                    {
                        return new ApiResponse<T>
                        {
                            Success = false,
                            Message = errorResponse.Message ?? GetMessageForStatusCode(response.StatusCode),
                            Errors = errorResponse.Errors ?? new List<string>(),
                            StatusCode = (int)response.StatusCode
                        };
                    }
                }
                catch (JsonException)
                {
                    // Se la deserializzazione fallisce, usa il contenuto raw come messaggio
                }
            }

            // Fallback per errori non strutturati
            return new ApiResponse<T>
            {
                Success = false,
                Message = GetMessageForStatusCode(response.StatusCode),
                Errors = new List<string> { content },
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Errore durante parsing risposta di errore per {Operation}", context);

            return new ApiResponse<T>
            {
                Success = false,
                Message = $"Errore di comunicazione: {response.StatusCode}",
                Errors = new List<string> { "Impossibile leggere la risposta del server" },
                StatusCode = (int)response.StatusCode
            };
        }
    }

    /// <summary>
    /// Determina se un errore può essere risolto con un retry
    /// </summary>
    public bool IsRetryableError(Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx => IsRetryableHttpException(httpEx),
            TaskCanceledException => true, // Timeout
            SocketException => true,
            TimeoutException => true,
            _ => false
        };
    }

    /// <summary>
    /// Determina se uno status code HTTP è retry-able
    /// </summary>
    public bool IsRetryableStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.RequestTimeout => true,           // 408
            HttpStatusCode.TooManyRequests => true,          // 429
            HttpStatusCode.InternalServerError => true,      // 500
            HttpStatusCode.BadGateway => true,               // 502
            HttpStatusCode.ServiceUnavailable => true,       // 503
            HttpStatusCode.GatewayTimeout => true,           // 504
            _ => false
        };
    }

    #region Private Helper Methods

    /// <summary>
    /// Gestisce HttpRequestException specifiche
    /// </summary>
    private async Task<ApiResponse<T>> HandleHttpRequestExceptionAsync<T>(HttpRequestException httpEx, string context)
    {
        // Analizza il messaggio per determinare il tipo di errore
        var message = httpEx.Message.ToLower();

        if (message.Contains("timeout") || message.Contains("timeout"))
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = "Timeout della richiesta. Verifica la connessione internet.",
                Errors = new List<string> { "La richiesta ha impiegato troppo tempo" },
                IsRetryable = true
            };
        }

        if (message.Contains("connection") || message.Contains("network"))
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = "Errore di connessione. Verifica la connessione internet.",
                Errors = new List<string> { "Impossibile contattare il server" },
                IsRetryable = true
            };
        }

        // Errore HTTP generico
        return new ApiResponse<T>
        {
            Success = false,
            Message = "Errore di comunicazione con il server",
            Errors = new List<string> { httpEx.Message },
            IsRetryable = IsRetryableHttpException(httpEx)
        };
    }

    /// <summary>
    /// Gestisce timeout ed operazioni cancellate
    /// </summary>
    private ApiResponse<T> HandleTimeoutException<T>(TaskCanceledException timeoutEx, string context)
    {
        var isTimeout = timeoutEx.InnerException is TimeoutException;

        return new ApiResponse<T>
        {
            Success = false,
            Message = isTimeout ?
                "Timeout della richiesta. Il server sta impiegando troppo tempo a rispondere." :
                "Operazione annullata",
            Errors = new List<string> { timeoutEx.Message },
            IsRetryable = isTimeout
        };
    }

    /// <summary>
    /// Gestisce errori di serializzazione JSON
    /// </summary>
    private ApiResponse<T> HandleJsonException<T>(JsonException jsonEx, string context)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = "Errore nel formato dei dati ricevuti dal server",
            Errors = new List<string> { "Formato risposta non valido", jsonEx.Message },
            IsRetryable = false
        };
    }

    /// <summary>
    /// Gestisce errori di autenticazione
    /// </summary>
    private ApiResponse<T> HandleAuthException<T>(UnauthorizedAccessException authEx, string context)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = "Accesso non autorizzato. Effettua nuovamente il login.",
            Errors = new List<string> { "Token di autenticazione non valido o scaduto" },
            IsRetryable = false,
            RequiresReauth = true
        };
    }

    /// <summary>
    /// Gestisce ApiException custom
    /// </summary>
    private ApiResponse<T> HandleApiException<T>(ApiException apiEx, string context)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = apiEx.Message,
            Errors = apiEx.Errors,
            StatusCode = apiEx.StatusCode,
            IsRetryable = apiEx.IsRetryable
        };
    }

    /// <summary>
    /// Gestisce eccezioni generiche non specificate
    /// </summary>
    private ApiResponse<T> HandleGenericException<T>(Exception ex, string context)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = "Si è verificato un errore imprevisto",
            Errors = new List<string> { ex.Message },
            IsRetryable = false
        };
    }

    /// <summary>
    /// Determina se una HttpRequestException è retry-able
    /// </summary>
    private bool IsRetryableHttpException(HttpRequestException httpEx)
    {
        var message = httpEx.Message.ToLower();
        return message.Contains("timeout") ||
               message.Contains("connection") ||
               message.Contains("network") ||
               message.Contains("dns");
    }

    /// <summary>
    /// Ottiene messaggio user-friendly per status code HTTP
    /// </summary>
    private string GetMessageForStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "Richiesta non valida",
            HttpStatusCode.Unauthorized => "Accesso non autorizzato",
            HttpStatusCode.Forbidden => "Accesso negato",
            HttpStatusCode.NotFound => "Risorsa non trovata",
            HttpStatusCode.Conflict => "Conflitto nei dati",
            HttpStatusCode.UnprocessableEntity => "Dati non validi",
            HttpStatusCode.TooManyRequests => "Troppe richieste. Riprova tra poco.",
            HttpStatusCode.InternalServerError => "Errore interno del server",
            HttpStatusCode.BadGateway => "Gateway non disponibile",
            HttpStatusCode.ServiceUnavailable => "Servizio temporaneamente non disponibile",
            HttpStatusCode.GatewayTimeout => "Timeout del gateway",
            _ => $"Errore HTTP {(int)statusCode}"
        };
    }

    #endregion
}



