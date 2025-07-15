using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using StreetCats.Client.Models.Api;
using StreetCats.Client.Services.Implementations;
using StreetCats.Client.Services.Exceptions;
using StreetCats.Client.Services.Configuration.Interfaces;
using StreetCats.Client.Services.Exceptions.Interfaces;

namespace StreetCats.Client.Services.Http.Handler;

/// <summary>
/// DelegatingHandler per retry automatico con exponential backoff
/// Riprova automaticamente le richieste fallite per errori temporanei
/// Implementa circuit breaker pattern per evitare sovraccarico
/// </summary>
public class RetryDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<RetryDelegatingHandler>? _logger;
    private readonly IAppSettings _appSettings;
    private readonly IApiExceptionHandler _exceptionHandler;

    // Circuit breaker state
    private DateTime _circuitBreakerLastFailTime = DateTime.MinValue;
    private int _consecutiveFailures = 0;
    private readonly int _circuitBreakerThreshold = 5;
    private readonly TimeSpan _circuitBreakerTimeout = TimeSpan.FromMinutes(1);

    public RetryDelegatingHandler(
        IAppSettings appSettings,
        IApiExceptionHandler exceptionHandler,
        ILogger<RetryDelegatingHandler>? logger = null)
    {
        _appSettings = appSettings;
        _exceptionHandler = exceptionHandler;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var maxRetries = _appSettings.Api.MaxRetries;

        // Skip retry se disabilitato
        if (maxRetries == 0)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Check circuit breaker
        if (IsCircuitBreakerOpen())
        {
            _logger?.LogWarning("⚡ Circuit breaker OPEN - richiesta bloccata per {Url}", request.RequestUri);
            throw new ApiException("Servizio temporaneamente non disponibile (circuit breaker)", 503, isRetryable: false);
        }

        Exception? lastException = null;
        HttpResponseMessage? lastResponse = null;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Clone request se non è il primo tentativo (la request può essere consumata)
                var requestToSend = attempt == 0 ? request : await CloneRequestAsync(request);

                var response = await base.SendAsync(requestToSend, cancellationToken);

                // Successo - reset circuit breaker
                if (response.IsSuccessStatusCode)
                {
                    ResetCircuitBreaker();

                    if (attempt > 0)
                    {
                        _logger?.LogInformation("✅ Richiesta riuscita al tentativo {Attempt} per {Url}",
                            attempt + 1, request.RequestUri);
                    }

                    return response;
                }

                // Errore HTTP - verifica se è retry-able
                if (!_exceptionHandler.IsRetryableStatusCode(response.StatusCode))
                {
                    _logger?.LogWarning("❌ Status {StatusCode} non retry-able per {Url}",
                        response.StatusCode, request.RequestUri);
                    return response;
                }

                lastResponse = response;

                // Se è l'ultimo tentativo, non fare delay
                if (attempt < maxRetries)
                {
                    var delay = _appSettings.Api.GetRetryDelay(attempt);

                    _logger?.LogWarning("🔄 Tentativo {Attempt}/{MaxRetries} fallito con {StatusCode} per {Url} - retry tra {Delay}ms",
                        attempt + 1, maxRetries + 1, response.StatusCode, request.RequestUri, delay.TotalMilliseconds);

                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;

                // Verifica se l'errore è retry-able
                if (!_exceptionHandler.IsRetryableError(ex))
                {
                    _logger?.LogWarning("❌ Errore {ErrorType} non retry-able per {Url}: {Message}",
                        ex.GetType().Name, request.RequestUri, ex.Message);
                    throw;
                }

                // Se è l'ultimo tentativo, non fare delay
                if (attempt < maxRetries)
                {
                    var delay = _appSettings.Api.GetRetryDelay(attempt);

                    _logger?.LogWarning("🔄 Tentativo {Attempt}/{MaxRetries} fallito con {ErrorType} per {Url} - retry tra {Delay}ms: {Message}",
                        attempt + 1, maxRetries + 1, ex.GetType().Name, request.RequestUri, delay.TotalMilliseconds, ex.Message);

                    try
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Se il cancellation token viene attivato durante il delay, interrompi
                        throw;
                    }
                }
            }
        }

        // Tutti i retry sono falliti - registra fallimento nel circuit breaker
        RecordCircuitBreakerFailure();

        // Rilancia ultima eccezione o ritorna ultima risposta
        if (lastException != null)
        {
            _logger?.LogError("❌ Tutti i {MaxRetries} retry falliti per {Url}: {ErrorType} - {Message}",
                maxRetries + 1, request.RequestUri, lastException.GetType().Name, lastException.Message);
            throw lastException;
        }

        if (lastResponse != null)
        {
            _logger?.LogError("❌ Tutti i {MaxRetries} retry falliti per {Url}: Status {StatusCode}",
                maxRetries + 1, request.RequestUri, lastResponse.StatusCode);
            return lastResponse;
        }

        // Caso edge - non dovrebbe mai accadere
        throw new InvalidOperationException("Retry failed without exception or response");
    }

    /// <summary>
    /// Clona una HttpRequestMessage per permettere retry
    /// (HttpRequestMessage può essere consumata una sola volta)
    /// </summary>
    private async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version
        };

        // Copia headers
        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copia properties
        foreach (var property in original.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(property.Key), property.Value);
        }

        // Copia content se presente
        if (original.Content != null)
        {
            var contentBytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            // Copia content headers
            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    #region Circuit Breaker Implementation

    /// <summary>
    /// Verifica se il circuit breaker è aperto (blocca le richieste)
    /// </summary>
    private bool IsCircuitBreakerOpen()
    {
        if (_consecutiveFailures < _circuitBreakerThreshold)
        {
            return false;
        }

        var timeSinceLastFailure = DateTime.UtcNow - _circuitBreakerLastFailTime;

        // Se è passato abbastanza tempo, prova a riaprire il circuito
        if (timeSinceLastFailure >= _circuitBreakerTimeout)
        {
            _logger?.LogInformation("🔧 Circuit breaker timeout scaduto - tentativo di riapertura");
            _consecutiveFailures = _circuitBreakerThreshold - 1; // Lascia una chance
            return false;
        }

        return true;
    }

    /// <summary>
    /// Registra un fallimento nel circuit breaker
    /// </summary>
    private void RecordCircuitBreakerFailure()
    {
        _consecutiveFailures++;
        _circuitBreakerLastFailTime = DateTime.UtcNow;

        if (_consecutiveFailures >= _circuitBreakerThreshold)
        {
            _logger?.LogWarning("⚡ Circuit breaker APERTO dopo {Failures} fallimenti consecutivi - timeout: {Timeout}",
                _consecutiveFailures, _circuitBreakerTimeout);
        }
    }

    /// <summary>
    /// Reset del circuit breaker dopo un successo
    /// </summary>
    private void ResetCircuitBreaker()
    {
        if (_consecutiveFailures > 0)
        {
            _logger?.LogInformation("✅ Circuit breaker RESET dopo successo (era a {Failures} fallimenti)",
                _consecutiveFailures);
            _consecutiveFailures = 0;
        }
    }

    #endregion
}

/// <summary>
/// Estensioni per registrare il RetryDelegatingHandler
/// </summary>
public static class RetryDelegatingHandlerExtensions
{
    /// <summary>
    /// Aggiunge RetryDelegatingHandler alla pipeline HTTP
    /// </summary>
    public static IHttpClientBuilder AddRetryPolicy(this IHttpClientBuilder builder)
    {
        return builder.AddHttpMessageHandler<RetryDelegatingHandler>();
    }
}

/// <summary>
/// Configurazione per circuit breaker (estendibile in futuro)
/// </summary>
public class CircuitBreakerOptions
{
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromMinutes(10);
}