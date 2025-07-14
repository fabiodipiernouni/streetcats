using System.ComponentModel.DataAnnotations;

namespace StreetCats.Client.Models.Api;

/// <summary>
/// Configurazione centralizzata per endpoint API e timeout
/// Strongly-typed configuration per sicurezza e manutenibilità
/// </summary>
public class ApiConfiguration
{
    public const string SectionName = "ApiSettings";

    /// <summary>
    /// Se true usa servizi mock, se false usa API reali
    /// </summary>
    public bool UseMockServices { get; set; } = true;

    /// <summary>
    /// URL base delle API backend (es. https://api.streetcats.it/v1)
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://localhost:3000/api";

    /// <summary>
    /// Timeout per le richieste HTTP in secondi
    /// </summary>
    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Numero massimo di retry per richieste fallite
    /// </summary>
    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay iniziale per exponential backoff (ms)
    /// </summary>
    [Range(100, 5000)]
    public int InitialRetryDelayMs { get; set; } = 500;

    /// <summary>
    /// Fattore moltiplicativo per exponential backoff
    /// </summary>
    [Range(1.1, 5.0)]
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// API key per servizi esterni (Google Maps, ecc.)
    /// NON inserire qui in produzione, usare Azure Key Vault o variabili ambiente
    /// </summary>
    public ExternalApisConfiguration ExternalApis { get; set; } = new();

    /// <summary>
    /// Configurazione logging per richieste HTTP
    /// </summary>
    public LoggingConfiguration Logging { get; set; } = new();

    /// <summary>
    /// Headers aggiuntivi da includere in tutte le richieste
    /// </summary>
    public Dictionary<string, string> DefaultHeaders { get; set; } = new()
    {
        { "User-Agent", "StreetCats-Blazor/1.0" },
        { "Accept", "application/json" },
        { "X-Client-Version", "1.0.0" }
    };

    /// <summary>
    /// Endpoint specifici per diversi servizi
    /// </summary>
    public EndpointsConfiguration Endpoints { get; set; } = new();
}

/// <summary>
/// Configurazione per API esterne (Google Maps, geocoding, ecc.)
/// </summary>
public class ExternalApisConfiguration
{
    /// <summary>
    /// Google Maps API Key per geocoding
    /// </summary>
    public string? GoogleMapsApiKey { get; set; }

    /// <summary>
    /// URL base per OpenStreetMap Nominatim (geocoding gratuito)
    /// </summary>
    public string NominatimBaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    /// <summary>
    /// Rate limit per Nominatim (richieste per secondo)
    /// </summary>
    public double NominatimRateLimit { get; set; } = 1.0;
}

/// <summary>
/// Configurazione logging HTTP
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// Abilita logging delle richieste HTTP
    /// </summary>
    public bool EnableRequestLogging { get; set; } = true;

    /// <summary>
    /// Abilita logging delle risposte HTTP
    /// </summary>
    public bool EnableResponseLogging { get; set; } = true;

    /// <summary>
    /// Abilita logging degli errori HTTP
    /// </summary>
    public bool EnableErrorLogging { get; set; } = true;

    /// <summary>
    /// Abilita misurazione performance delle richieste
    /// </summary>
    public bool EnablePerformanceLogging { get; set; } = true;

    /// <summary>
    /// Log solo richieste che superano questa soglia (ms)
    /// </summary>
    public int SlowRequestThresholdMs { get; set; } = 2000;
}

/// <summary>
/// Configurazione endpoint specifici
/// </summary>
public class EndpointsConfiguration
{
    // Auth endpoints
    public string AuthLogin { get; set; } = "/auth/login";
    public string AuthRegister { get; set; } = "/auth/register";
    public string AuthRefresh { get; set; } = "/auth/refresh";
    public string AuthProfile { get; set; } = "/auth/me";

    // Cat endpoints
    public string CatsBase { get; set; } = "/cats";
    public string CatsById { get; set; } = "/cats/{id}";
    public string CatsSearch { get; set; } = "/cats/search";
    public string CatsInArea { get; set; } = "/cats/area";

    // Comment endpoints
    public string Comments { get; set; } = "/cats/{catId}/comments";
    public string CommentsById { get; set; } = "/cats/{catId}/comments/{commentId}";

    // Upload endpoints
    public string Upload { get; set; } = "/upload";
    public string UploadImages { get; set; } = "/upload/images";

    // Helper method per costruire URL completi
    public string BuildUrl(string baseUrl, string endpoint, Dictionary<string, object>? parameters = null)
    {
        var url = baseUrl.TrimEnd('/') + endpoint;

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                url = url.Replace($"{{{param.Key}}}", param.Value.ToString());
            }
        }

        return url;
    }
}

/// <summary>
/// Estensioni per validazione configurazione
/// </summary>
public static class ApiConfigurationExtensions
{
    /// <summary>
    /// Valida la configurazione e restituisce eventuali errori
    /// </summary>
    public static List<string> Validate(this ApiConfiguration config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            errors.Add("BaseUrl è obbligatorio");
        }
        else if (!Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out _))
        {
            errors.Add("BaseUrl deve essere un URL valido");
        }

        if (config.TimeoutSeconds < 5 || config.TimeoutSeconds > 300)
        {
            errors.Add("TimeoutSeconds deve essere tra 5 e 300");
        }

        if (config.MaxRetries < 0 || config.MaxRetries > 10)
        {
            errors.Add("MaxRetries deve essere tra 0 e 10");
        }

        return errors;
    }

    /// <summary>
    /// Ottiene TimeSpan per timeout
    /// </summary>
    public static TimeSpan GetTimeout(this ApiConfiguration config)
    {
        return TimeSpan.FromSeconds(config.TimeoutSeconds);
    }

    /// <summary>
    /// Ottiene TimeSpan per retry delay
    /// </summary>
    public static TimeSpan GetRetryDelay(this ApiConfiguration config, int retryAttempt)
    {
        var delay = config.InitialRetryDelayMs * Math.Pow(config.BackoffMultiplier, retryAttempt);
        return TimeSpan.FromMilliseconds(Math.Min(delay, 30000)); // Max 30 secondi
    }
}