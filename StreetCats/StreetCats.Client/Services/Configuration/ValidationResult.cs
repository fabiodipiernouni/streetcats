using System.Collections.Generic;

namespace StreetCats.Client.Services.Configuration;

/// <summary>
/// Risultato della validazione configurazione
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Aggiunge un errore al risultato
    /// </summary>
    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    /// <summary>
    /// Aggiunge un warning al risultato
    /// </summary>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    /// <summary>
    /// Restituisce un summary del risultato
    /// </summary>
    public string GetSummary()
    {
        var summary = IsValid ? "✅ Configurazione valida" : $"❌ {Errors.Count} errori di configurazione";

        if (Warnings.Count > 0)
        {
            summary += $", {Warnings.Count} warning";
        }

        return summary;
    }
}