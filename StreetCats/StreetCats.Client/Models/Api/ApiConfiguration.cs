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

    /// <summary>
    /// Valida la configurazione e restituisce eventuali errori
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            errors.Add("BaseUrl è obbligatorio");
        }
        else if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
        {
            errors.Add("BaseUrl deve essere un URL valido");
        }

        if (TimeoutSeconds < 5 || TimeoutSeconds > 300)
        {
            errors.Add("TimeoutSeconds deve essere tra 5 e 300");
        }

        if (MaxRetries < 0 || MaxRetries > 10)
        {
            errors.Add("MaxRetries deve essere tra 0 e 10");
        }

        return errors;
    }

    /// <summary>
    /// Ottiene TimeSpan per timeout
    /// </summary>
    public TimeSpan GetTimeout()
    {
        return TimeSpan.FromSeconds(TimeoutSeconds);
    }

    /// <summary>
    /// Ottiene TimeSpan per retry delay
    /// </summary>
    public TimeSpan GetRetryDelay(int retryAttempt)
    {
        var delay = InitialRetryDelayMs * Math.Pow(BackoffMultiplier, retryAttempt);
        return TimeSpan.FromMilliseconds(Math.Min(delay, 30000)); // Max 30 secondi
    }
}
