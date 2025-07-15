
namespace StreetCats.Client.Models.Api;

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