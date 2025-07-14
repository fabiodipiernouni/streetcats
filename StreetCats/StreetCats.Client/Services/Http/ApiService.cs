using StreetCats.Client.Models.DTOs;
using StreetCats.Client.Services.Configuration;
using StreetCats.Client.Services.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StreetCats.Client.Services.Http;

/// <summary>
/// Classe base per tutti i servizi API che consumano endpoint REST
/// Fornisce metodi unificati per serializzazione, deserializzazione e gestione errori
/// </summary>
public abstract class ApiService
{
    protected readonly IAuthenticatedHttpClient HttpClient;
    protected readonly IAppSettings AppSettings;
    protected readonly IApiExceptionHandler ExceptionHandler;
    protected readonly ILogger? Logger;

    protected ApiService(
        IAuthenticatedHttpClient httpClient,
        IAppSettings appSettings,
        IApiExceptionHandler exceptionHandler,
        ILogger? logger = null)
    {
        HttpClient = httpClient;
        AppSettings = appSettings;
        ExceptionHandler = exceptionHandler;
        Logger = logger;
    }

    #region Protected GET Methods

    /// <summary>
    /// Esegue GET e deserializza la risposta
    /// </summary>
    protected async Task<ApiResponse<T>> GetAsync<T>(string endpoint, string? operation = null)
    {
        var operationName = operation ?? $"GET {endpoint}";

        try
        {
            Logger?.LogDebug("🔵 {Operation} - inizio", operationName);

            var response = await HttpClient.GetAsync(endpoint);
            return await ProcessResponseAsync<T>(response, operationName);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ {Operation} - errore", operationName);
            return await ExceptionHandler.HandleExceptionAsync<T>(ex, operationName);
        }
    }

    /// <summary>
    /// Esegue GET con parametri query
    /// </summary>
    protected async Task<ApiResponse<T>> GetAsync<T>(string endpoint, object queryParams, string? operation = null)
    {
        var url = BuildUrlWithQuery(endpoint, queryParams);
        return await GetAsync<T>(url, operation);
    }

    /// <summary>
    /// Esegue GET con parametri query come dictionary
    /// </summary>
    protected async Task<ApiResponse<T>> GetAsync<T>(string endpoint, Dictionary<string, object?> queryParams, string? operation = null)
    {
        var url = BuildUrlWithQuery(endpoint, queryParams);
        return await GetAsync<T>(url, operation);
    }

    #endregion

    #region Protected POST Methods

    /// <summary>
    /// Esegue POST con payload JSON
    /// </summary>
    protected async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? payload = null, string? operation = null)
    {
        var operationName = operation ?? $"POST {endpoint}";

        try
        {
            Logger?.LogDebug("🟡 {Operation} - inizio", operationName);

            HttpResponseMessage response;

            if (payload != null)
            {
                response = await HttpClient.PostAsJsonAsync(endpoint, payload);
            }
            else
            {
                response = await HttpClient.PostAsync(endpoint, null);
            }

            return await ProcessResponseAsync<T>(response, operationName);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ {Operation} - errore", operationName);
            return await ExceptionHandler.HandleExceptionAsync<T>(ex, operationName);
        }
    }

    /// <summary>
    /// Esegue POST con content personalizzato
    /// </summary>
    protected async Task<ApiResponse<T>> PostAsync<T>(string endpoint, HttpContent content, string? operation = null)
    {
        var operationName = operation ?? $"POST {endpoint}";

        try
        {
            Logger?.LogDebug("🟡 {Operation} - inizio con content personalizzato", operationName);

            var response = await HttpClient.PostAsync(endpoint, content);
            return await ProcessResponseAsync<T>(response, operationName);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ {Operation} - errore", operationName);
            return await ExceptionHandler.HandleExceptionAsync<T>(ex, operationName);
        }
    }

    #endregion

    #region Protected PUT Methods

    /// <summary>
    /// Esegue PUT con payload JSON
    /// </summary>
    protected async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object payload, string? operation = null)
    {
        var operationName = operation ?? $"PUT {endpoint}";

        try
        {
            Logger?.LogDebug("🟠 {Operation} - inizio", operationName);

            var response = await HttpClient.PutAsJsonAsync(endpoint, payload);
            return await ProcessResponseAsync<T>(response, operationName);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ {Operation} - errore", operationName);
            return await ExceptionHandler.HandleExceptionAsync<T>(ex, operationName);
        }
    }

    #endregion

    #region Protected DELETE Methods

    /// <summary>
    /// Esegue DELETE
    /// </summary>
    protected async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint, string? operation = null)
    {
        var operationName = operation ?? $"DELETE {endpoint}";

        try
        {
            Logger?.LogDebug("🔴 {Operation} - inizio", operationName);

            var response = await HttpClient.DeleteAsync(endpoint);
            return await ProcessResponseAsync<T>(response, operationName);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ {Operation} - errore", operationName);
            return await ExceptionHandler.HandleExceptionAsync<T>(ex, operationName);
        }
    }

    /// <summary>
    /// Esegue DELETE e ritorna solo bool per successo
    /// </summary>
    protected async Task<ApiResponse<bool>> DeleteAsync(string endpoint, string? operation = null)
    {
        var result = await DeleteAsync<object>(endpoint, operation);

        return new ApiResponse<bool>
        {
            Success = result.Success,
            Message = result.Message,
            Errors = result.Errors,
            Data = result.Success,
            StatusCode = result.StatusCode,
            IsRetryable = result.IsRetryable
        };
    }

    #endregion

    #region Response Processing

    /// <summary>
    /// Processa HttpResponseMessage e deserializza in ApiResponse<T>
    /// </summary>
    protected async Task<ApiResponse<T>> ProcessResponseAsync<T>(HttpResponseMessage response, string operation)
    {
        try
        {
            // Se è un errore HTTP, usa ExceptionHandler
            if (!response.IsSuccessStatusCode)
            {
                return await ExceptionHandler.HandleHttpErrorAsync<T>(response, operation);
            }

            // Leggi contenuto
            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                // Risposta vuota per operazioni come DELETE
                return new ApiResponse<T>
                {
                    Success = true,
                    Message = "Operazione completata con successo",
                    StatusCode = (int)response.StatusCode
                };
            }

            // Tenta deserializzazione diretta del tipo T
            if (typeof(T) == typeof(string))
            {
                return new ApiResponse<T>
                {
                    Success = true,
                    Data = (T)(object)content,
                    StatusCode = (int)response.StatusCode
                };
            }

            // Prova prima a deserializzare come ApiResponse<T> (formato standard)
            try
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content, GetJsonOptions());
                if (apiResponse != null)
                {
                    apiResponse.StatusCode = (int)response.StatusCode;
                    return apiResponse;
                }
            }
            catch (JsonException)
            {
                // Se fallisce, prova deserializzazione diretta del tipo T
            }

            // Deserializzazione diretta del tipo T
            var data = JsonSerializer.Deserialize<T>(content, GetJsonOptions());

            return new ApiResponse<T>
            {
                Success = true,
                Message = "Operazione completata con successo",
                Data = data,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (JsonException jsonEx)
        {
            Logger?.LogError(jsonEx, "❌ Errore deserializzazione JSON per {Operation}", operation);

            return new ApiResponse<T>
            {
                Success = false,
                Message = "Errore nel formato della risposta del server",
                Errors = new List<string> { "Formato JSON non valido", jsonEx.Message },
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ Errore processing risposta per {Operation}", operation);
            return await ExceptionHandler.HandleExceptionAsync<T>(ex, operation);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Costruisce URL con parametri query da oggetto
    /// </summary>
    protected string BuildUrlWithQuery(string endpoint, object queryParams)
    {
        if (queryParams == null) return endpoint;

        var properties = queryParams.GetType().GetProperties()
            .Where(p => p.GetValue(queryParams) != null)
            .Select(p => $"{p.Name.ToLower()}={Uri.EscapeDataString(p.GetValue(queryParams)?.ToString() ?? "")}");

        var queryString = string.Join("&", properties);

        return string.IsNullOrEmpty(queryString) ? endpoint : $"{endpoint}?{queryString}";
    }

    /// <summary>
    /// Costruisce URL con parametri query da dictionary
    /// </summary>
    protected string BuildUrlWithQuery(string endpoint, Dictionary<string, object?> queryParams)
    {
        if (queryParams == null || !queryParams.Any()) return endpoint;

        var queryString = string.Join("&",
            queryParams
                .Where(kvp => kvp.Value != null)
                .Select(kvp => $"{kvp.Key.ToLower()}={Uri.EscapeDataString(kvp.Value?.ToString() ?? "")}"));

        return string.IsNullOrEmpty(queryString) ? endpoint : $"{endpoint}?{queryString}";
    }

    /// <summary>
    /// Costruisce endpoint con parametri path (es. /cats/{id})
    /// </summary>
    protected string BuildEndpoint(string template, Dictionary<string, object> pathParams)
    {
        var result = template;

        foreach (var param in pathParams)
        {
            result = result.Replace($"{{{param.Key}}}", Uri.EscapeDataString(param.Value.ToString() ?? ""));
        }

        return result;
    }

    /// <summary>
    /// Ottiene JsonSerializerOptions configurate per STREETCATS
    /// </summary>
    protected JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                // Aggiungi converters personalizzati se necessario
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
    }

    /// <summary>
    /// Crea HttpContent JSON da oggetto
    /// </summary>
    protected StringContent CreateJsonContent(object obj)
    {
        var json = JsonSerializer.Serialize(obj, GetJsonOptions());
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Log standard per inizio operazione
    /// </summary>
    protected void LogOperationStart(string operation, object? parameters = null)
    {
        if (parameters != null)
        {
            Logger?.LogDebug("🚀 {Operation} - inizio con parametri: {Parameters}",
                operation, JsonSerializer.Serialize(parameters, GetJsonOptions()));
        }
        else
        {
            Logger?.LogDebug("🚀 {Operation} - inizio", operation);
        }
    }

    /// <summary>
    /// Log standard per fine operazione
    /// </summary>
    protected void LogOperationSuccess(string operation, object? result = null)
    {
        if (result != null)
        {
            Logger?.LogDebug("✅ {Operation} - successo: {Result}",
                operation, JsonSerializer.Serialize(result, GetJsonOptions()));
        }
        else
        {
            Logger?.LogDebug("✅ {Operation} - successo", operation);
        }
    }

    #endregion
}

/// <summary>
/// Converter personalizzato per enum come stringhe
/// </summary>
public class JsonStringEnumConverter : JsonConverter<Enum>
{
    private readonly JsonNamingPolicy? _namingPolicy;

    public JsonStringEnumConverter(JsonNamingPolicy? namingPolicy = null)
    {
        _namingPolicy = namingPolicy;
    }

    public override Enum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (Enum.TryParse(typeToConvert, stringValue, true, out var result))
            {
                return (Enum)result;
            }
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, Enum value, JsonSerializerOptions options)
    {
        var stringValue = value.ToString();
        if (_namingPolicy != null)
        {
            stringValue = _namingPolicy.ConvertName(stringValue);
        }
        writer.WriteStringValue(stringValue);
    }
}