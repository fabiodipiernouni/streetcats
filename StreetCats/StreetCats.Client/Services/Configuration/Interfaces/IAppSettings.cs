using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StreetCats.Client.Models.Api;
using System.ComponentModel.DataAnnotations;
using StreetCats.Client.Services.Configuration;

namespace StreetCats.Client.Services.Configuration.Interfaces;

/// <summary>
/// Servizio per la gestione strongly-typed della configurazione dell'applicazione
/// Fornisce accesso type-safe alle impostazioni con validazione automatica
/// </summary>
public interface IAppSettings
{
    /// <summary>
    /// Configurazione API
    /// </summary>
    ApiConfiguration Api { get; }

    /// <summary>
    /// Verifica se l'applicazione è in modalità sviluppo (usa servizi mock)
    /// </summary>
    bool IsDevelopmentMode { get; }

    /// <summary>
    /// Verifica se l'applicazione è in modalità produzione (usa API reali)
    /// </summary>
    bool IsProductionMode { get; }

    /// <summary>
    /// Valida tutta la configurazione
    /// </summary>
    ValidationResult ValidateConfiguration();
}