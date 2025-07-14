using StreetCats.Client.Services.Implementation;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreetCats.Client.Services.Http;

/// <summary>
/// DelegatingHandler per logging automatico di tutte le richieste HTTP
/// Traccia performance, errori e debug information per sviluppo e troubleshooting
/// </summary>
public class LoggingDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingDelegatingHandler>? _logger;
    private readonly IAppSettings _appSettings;

    public LoggingDelegatingHandler(IAppSettings appSettings, ILogger<LoggingDelegatingHandler>? logger = null)
    {
        _appSettings = appSettings;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var config = _appSettings.Api.Logging;

        // Skip logging se disabilitato
        if (!config.EnableRequestLogging && !config.EnableResponseLogging && !config.EnablePerformanceLogging)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var requestId = Guid.NewGuid().ToString("N")[..8];
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Log richiesta
            if (config.EnableRequestLogging)
            {
                await LogRequestAsync(request, requestId);
            }

            // Esegui richiesta
            var response = await base.SendAsync(request, cancellationToken);

            stopwatch.Stop();

            // Log risposta
            if (config.EnableResponseLogging)
            {
                await LogResponseAsync(response, requestId, stopwatch.ElapsedMilliseconds);
            }

            // Log performance se necessario
            if (config.EnablePerformanceLogging && stopwatch.ElapsedMilliseconds > config.SlowRequestThresholdMs)
            {
                LogSlowRequest(request, stopwatch.ElapsedMilliseconds, requestId);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log errore
            if (config.EnableErrorLogging)
            {
                LogRequestError(request, ex, stopwatch.ElapsedMilliseconds, requestId);
            }

            throw;
        }
    }

    /// <summary>
    /// Log dettagli della richiesta HTTP
    /// </summary>
    private async Task LogRequestAsync(HttpRequestMessage request, string requestId)
    {
        try
        {
            var logBuilder = new StringBuilder();
            logBuilder.AppendLine($"🔵 HTTP Request [{requestId}]");
            logBuilder.AppendLine($"   Method: {request.Method}");
            logBuilder.AppendLine($"   URL: {request.RequestUri}");

            // Headers interessanti (escludi sensitive data)
            if (request.Headers.Any())
            {
                logBuilder.AppendLine("   Headers:");
                foreach (var header in request.Headers)
                {
                    if (IsSafeHeader(header.Key))
                    {
                        logBuilder.AppendLine($"     {header.Key}: {string.Join(", ", header.Value)}");
                    }
                    else
                    {
                        logBuilder.AppendLine($"     {header.Key}: [HIDDEN]");
                    }
                }
            }

            // Content length se presente
            if (request.Content != null)
            {
                var contentLength = request.Content.Headers.ContentLength;
                if (contentLength.HasValue)
                {
                    logBuilder.AppendLine($"   Content-Length: {contentLength.Value} bytes");
                }

                var contentType = request.Content.Headers.ContentType?.ToString();
                if (!string.IsNullOrEmpty(contentType))
                {
                    logBuilder.AppendLine($"   Content-Type: {contentType}");
                }

                // Log body per richieste piccole (solo in development)
                if (_appSettings.IsDevelopmentMode && contentLength < 1024)
                {
                    try
                    {
                        var content = await request.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(content))
                        {
                            logBuilder.AppendLine($"   Body: {content}");
                        }
                    }
                    catch
                    {
                        logBuilder.AppendLine("   Body: [Cannot read content]");
                    }
                }
            }

            _logger?.LogInformation(logBuilder.ToString());
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "⚠️ Errore durante logging richiesta [{RequestId}]", requestId);
        }
    }

    /// <summary>
    /// Log dettagli della risposta HTTP
    /// </summary>
    private async Task LogResponseAsync(HttpResponseMessage response, string requestId, long elapsedMs)
    {
        try
        {
            var logBuilder = new StringBuilder();
            var statusEmoji = response.IsSuccessStatusCode ? "🟢" : "🔴";

            logBuilder.AppendLine($"{statusEmoji} HTTP Response [{requestId}] - {elapsedMs}ms");
            logBuilder.AppendLine($"   Status: {(int)response.StatusCode} {response.StatusCode}");

            // Headers interessanti
            if (response.Headers.Any())
            {
                logBuilder.AppendLine("   Headers:");
                foreach (var header in response.Headers)
                {
                    if (IsSafeHeader(header.Key))
                    {
                        logBuilder.AppendLine($"     {header.Key}: {string.Join(", ", header.Value)}");
                    }
                }
            }

            // Content info
            if (response.Content != null)
            {
                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength.HasValue)
                {
                    logBuilder.AppendLine($"   Content-Length: {contentLength.Value} bytes");
                }

                var contentType = response.Content.Headers.ContentType?.ToString();
                if (!string.IsNullOrEmpty(contentType))
                {
                    logBuilder.AppendLine($"   Content-Type: {contentType}");
                }

                // Log body per errori o in development mode
                if (!response.IsSuccessStatusCode || (_appSettings.IsDevelopmentMode && contentLength < 1024))
                {
                    try
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(content))
                        {
                            var truncatedContent = content.Length > 500 ? content[..500] + "..." : content;
                            logBuilder.AppendLine($"   Body: {truncatedContent}");
                        }
                    }
                    catch
                    {
                        logBuilder.AppendLine("   Body: [Cannot read content]");
                    }
                }
            }

            // Performance categorization
            var perfCategory = elapsedMs switch
            {
                < 200 => "⚡ Very Fast",
                < 500 => "🚀 Fast",
                < 1000 => "🏃 Acceptable",
                < 3000 => "🐌 Slow",
                _ => "🐢 Very Slow"
            };
            logBuilder.AppendLine($"   Performance: {perfCategory}");

            var logLevel = response.IsSuccessStatusCode ? LogLevel.Information : LogLevel.Warning;
            _logger?.Log(logLevel, logBuilder.ToString());
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "⚠️ Errore durante logging risposta [{RequestId}]", requestId);
        }
    }

    /// <summary>
    /// Log richieste lente per monitoraggio performance
    /// </summary>
    private void LogSlowRequest(HttpRequestMessage request, long elapsedMs, string requestId)
    {
        _logger?.LogWarning(
            "🐌 SLOW REQUEST [{RequestId}]: {Method} {Url} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
            requestId,
            request.Method,
            request.RequestUri,
            elapsedMs,
            _appSettings.Api.Logging.SlowRequestThresholdMs
        );
    }

    /// <summary>
    /// Log errori delle richieste HTTP
    /// </summary>
    private void LogRequestError(HttpRequestMessage request, Exception ex, long elapsedMs, string requestId)
    {
        _logger?.LogError(ex,
            "❌ HTTP ERROR [{RequestId}]: {Method} {Url} failed after {ElapsedMs}ms - {ErrorType}: {ErrorMessage}",
            requestId,
            request.Method,
            request.RequestUri,
            elapsedMs,
            ex.GetType().Name,
            ex.Message
        );
    }

    /// <summary>
    /// Determina se un header può essere loggato in sicurezza (non contiene dati sensibili)
    /// </summary>
    private static bool IsSafeHeader(string headerName)
    {
        var sensitiveHeaders = new[]
        {
            "authorization",
            "cookie",
            "set-cookie",
            "x-api-key",
            "x-auth-token",
            "authentication",
            "proxy-authorization"
        };

        return !sensitiveHeaders.Contains(headerName.ToLowerInvariant());
    }
}

/// <summary>
/// Estensioni per registrare il LoggingDelegatingHandler
/// </summary>
public static class LoggingDelegatingHandlerExtensions
{
    /// <summary>
    /// Aggiunge LoggingDelegatingHandler alla pipeline HTTP
    /// </summary>
    public static IHttpClientBuilder AddLogging(this IHttpClientBuilder builder)
    {
        return builder.AddHttpMessageHandler<LoggingDelegatingHandler>();
    }
}