using System;
using System.Collections.Generic;

namespace StreetCats.Client.Models.DTOs;


/// <summary>
/// Wrapper generico per le risposte delle API REST
/// Versione aggiornata con supporto per retry, status codes e autenticazione
/// </summary>
/// <typeparam name="T">Tipo dei dati restituiti</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indica se l'operazione è riuscita
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Messaggio descrittivo del risultato
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Dati restituiti dall'API (se Success = true)
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Lista di errori dettagliati (se Success = false)
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Status code HTTP della risposta
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Indica se l'errore può essere risolto con un retry
    /// </summary>
    public bool IsRetryable { get; set; } = false;

    /// <summary>
    /// Indica se l'errore richiede una nuova autenticazione
    /// </summary>
    public bool RequiresReauth { get; set; } = false;

    /// <summary>
    /// Timestamp della risposta
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID di tracciamento per debugging (se fornito dal server)
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Metadati aggiuntivi per paginazione, cache, ecc.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Costruttore per risposta di successo
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string message = "Operazione completata con successo")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = 200
        };
    }

    /// <summary>
    /// Costruttore per risposta di errore
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null, int? statusCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>(),
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Costruttore per risposta di errore con singolo errore
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string message, string error, int? statusCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = new List<string> { error },
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Costruttore per errore di autenticazione
    /// </summary>
    public static ApiResponse<T> AuthErrorResponse(string message = "Accesso non autorizzato")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = new List<string> { "Token di autenticazione non valido o scaduto" },
            StatusCode = 401,
            RequiresReauth = true
        };
    }

    /// <summary>
    /// Costruttore per errore retry-able
    /// </summary>
    public static ApiResponse<T> RetryableErrorResponse(string message, string error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = new List<string> { error },
            IsRetryable = true
        };
    }

    /// <summary>
    /// Aggiunge un errore alla lista
    /// </summary>
    public void AddError(string error)
    {
        Errors.Add(error);
        Success = false;
    }

    /// <summary>
    /// Aggiunge metadato
    /// </summary>
    public void AddMetadata(string key, object value)
    {
        Metadata ??= new Dictionary<string, object>();
        Metadata[key] = value;
    }

    /// <summary>
    /// Ottiene metadato tipizzato
    /// </summary>
    public TMetadata? GetMetadata<TMetadata>(string key)
    {
        if (Metadata?.TryGetValue(key, out var value) == true && value is TMetadata typed)
        {
            return typed;
        }
        return default;
    }

    /// <summary>
    /// Converte in ApiResponse di tipo diverso mantenendo metadati
    /// </summary>
    public ApiResponse<TNew> ConvertTo<TNew>(TNew? newData = default)
    {
        return new ApiResponse<TNew>
        {
            Success = Success,
            Message = Message,
            Data = newData,
            Errors = new List<string>(Errors),
            StatusCode = StatusCode,
            IsRetryable = IsRetryable,
            RequiresReauth = RequiresReauth,
            Timestamp = Timestamp,
            TraceId = TraceId,
            Metadata = Metadata != null ? new Dictionary<string, object>(Metadata) : null
        };
    }

    /// <summary>
    /// Ottiene status HTTP come enum
    /// </summary>
    public System.Net.HttpStatusCode? GetHttpStatusCode()
    {
        return StatusCode.HasValue ? (System.Net.HttpStatusCode)StatusCode.Value : null;
    }

    /// <summary>
    /// Verifica se la risposta indica un errore client (4xx)
    /// </summary>
    public bool IsClientError => StatusCode >= 400 && StatusCode < 500;

    /// <summary>
    /// Verifica se la risposta indica un errore server (5xx)
    /// </summary>
    public bool IsServerError => StatusCode >= 500 && StatusCode < 600;

    /// <summary>
    /// Verifica se la risposta è un errore di autenticazione/autorizzazione
    /// </summary>
    public bool IsAuthError => StatusCode == 401 || StatusCode == 403;

    /// <summary>
    /// Override ToString per debugging
    /// </summary>
    public override string ToString()
    {
        var status = Success ? "SUCCESS" : "ERROR";
        var code = StatusCode.HasValue ? $" [{StatusCode}]" : "";
        return $"{status}{code}: {Message}";
    }
}


/// <summary>
/// Response per operazioni CRUD con metadati di paginazione
/// </summary>
public class PagedApiResponse<T> : ApiResponse<List<T>>
{
    /// <summary>
    /// Numero di pagina corrente (1-based)
    /// </summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// Numero di elementi per pagina
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Numero totale di elementi
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Numero totale di pagine
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Indica se esiste una pagina precedente
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Indica se esiste una pagina successiva
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Costruttore per risposta paginata di successo
    /// </summary>
    public static PagedApiResponse<T> SuccessResponse(
        List<T> data,
        int currentPage,
        int pageSize,
        int totalCount,
        string message = "Dati caricati con successo")
    {
        return new PagedApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            CurrentPage = currentPage,
            PageSize = pageSize,
            TotalCount = totalCount,
            StatusCode = 200
        };
    }
}