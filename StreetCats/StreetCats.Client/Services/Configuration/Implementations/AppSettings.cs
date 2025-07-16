using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using StreetCats.Client.Models.Api;
using StreetCats.Client.Services.Configuration.Interfaces;

namespace StreetCats.Client.Services.Configuration.Implementations;

/// <summary>
/// Implementazione del servizio di configurazione
/// </summary>
public class AppSettings : IAppSettings
{
    private readonly ApiConfiguration _apiConfig;

    public AppSettings(IConfiguration configuration)
    {
        // Carica configurazione API
        _apiConfig = new ApiConfiguration();
        configuration.GetSection(ApiConfiguration.SectionName).Bind(_apiConfig);

        // Valida configurazione al caricamento
        var validationResult = ValidateConfiguration();
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors);
            throw new InvalidOperationException($"Configurazione non valida: {errors}");
        }

        Console.WriteLine($"AppSettings caricato: Modalità {(IsDevelopmentMode ? "SVILUPPO" : "PRODUZIONE")}");
        Console.WriteLine($"   • Base URL: {_apiConfig.BaseUrl}");
        Console.WriteLine($"   • Timeout: {_apiConfig.TimeoutSeconds}s");
        Console.WriteLine($"   • Max Retries: {_apiConfig.MaxRetries}");
    }

    public ApiConfiguration Api => _apiConfig;

    public bool IsDevelopmentMode => _apiConfig.UseMockServices;

    public bool IsProductionMode => !_apiConfig.UseMockServices;

    /// <summary>
    /// Valida tutta la configurazione usando Data Annotations e logica custom
    /// </summary>
    public ValidationResult ValidateConfiguration()
    {
        var result = new ValidationResult();

        // Validazione usando Data Annotations
        var validationContext = new ValidationContext(_apiConfig);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        if (!Validator.TryValidateObject(_apiConfig, validationContext, validationResults, true))
        {
            foreach (var validationError in validationResults)
            {
                result.Errors.Add(validationError.ErrorMessage ?? "Errore di validazione sconosciuto");
            }
        }

        // Validazione custom usando extension method
        var customErrors = _apiConfig.Validate();
        result.Errors.AddRange(customErrors);

        // Validazioni specifiche per STREETCATS
        ValidateStreetCatsSpecific(result);

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    /// <summary>
    /// Validazioni specifiche per l'applicazione STREETCATS
    /// </summary>
    private void ValidateStreetCatsSpecific(ValidationResult result)
    {
        // Verifica che in produzione non si usino servizi mock
        if (IsProductionMode && _apiConfig.BaseUrl.Contains("localhost"))
        {
            result.Errors.Add("In produzione non utilizzare localhost come BaseUrl");
        }

        // Verifica che gli endpoint siano configurati
        if (string.IsNullOrWhiteSpace(_apiConfig.Endpoints.AuthLogin))
        {
            result.Errors.Add("Endpoint AuthLogin non configurato");
        }

        if (string.IsNullOrWhiteSpace(_apiConfig.Endpoints.CatsBase))
        {
            result.Errors.Add("Endpoint CatsBase non configurato");
        }

        // Avvisi per configurazioni sub-ottimali
        if (_apiConfig.TimeoutSeconds < 10)
        {
            Console.WriteLine("AVVISO: Timeout molto basso, potrebbe causare errori di rete");
        }

        if (_apiConfig.MaxRetries == 0)
        {
            Console.WriteLine("AVVISO: Retry disabilitati, l'app potrebbe essere meno resiliente");
        }
    }
}