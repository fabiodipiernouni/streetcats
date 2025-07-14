using StreetCats.Client.Models;
using StreetCats.Client.Models.DTOs;
using StreetCats.Client.Services.Configuration;
using StreetCats.Client.Services.Http;
using StreetCats.Client.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StreetCats.Client.Services.Implementation;

/// <summary>
/// Implementazione REALE del servizio per la gestione dei gatti
/// Comunica con le API REST del backend per operazioni CRUD sui gatti
/// </summary>
public class CatService : ApiService, ICatService
{
    private readonly IAuthService _authService;

    public CatService(
        IAuthenticatedHttpClient httpClient,
        IAppSettings appSettings,
        IApiExceptionHandler exceptionHandler,
        IAuthService authService,
        ILogger<CatService>? logger = null)
        : base(httpClient, appSettings, exceptionHandler, logger)
    {
        _authService = authService;
    }

    #region Public API Methods

    /// <summary>
    /// Ottiene tutti i gatti dal server
    /// </summary>
    public async Task<ApiResponse<List<Cat>>> GetAllCatsAsync()
    {
        try
        {
            LogOperationStart("GetAllCats");

            var endpoint = AppSettings.Api.Endpoints.CatsBase;
            var response = await GetAsync<List<Cat>>(endpoint, "GetAllCats API");

            if (response.Success && response.Data != null)
            {
                Logger?.LogInformation("✅ Caricati {Count} gatti dal server", response.Data.Count);
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ Errore durante caricamento tutti i gatti");
            return await ExceptionHandler.HandleExceptionAsync<List<Cat>>(ex, "GetAllCats");
        }
    }

    /// <summary>
    /// Ricerca gatti in un'area geografica specifica
    /// </summary>
    public async Task<ApiResponse<List<Cat>>> GetCatsInAreaAsync(double latitude, double longitude, double radiusKm = 5.0)
    {
        try
        {
            LogOperationStart("GetCatsInArea", new { latitude, longitude, radiusKm });

            var queryParams = new Dictionary<string, object?>
            {
                { "lat", latitude },
                { "lng", longitude },
                { "radius", radiusKm }
            };

            var endpoint = AppSettings.Api.Endpoints.CatsInArea;
            var response = await GetAsync<List<Cat>>(endpoint, queryParams, "GetCatsInArea API");

            if (response.Success && response.Data != null)
            {
                Logger?.LogInformation("✅ Trovati {Count} gatti nel raggio di {RadiusKm} km da ({Lat}, {Lng})",
                    response.Data.Count, radiusKm, latitude, longitude);
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ Errore durante ricerca gatti in area: {Lat}, {Lng}, {Radius}km",
                latitude, longitude, radiusKm);
            return await ExceptionHandler.HandleExceptionAsync<List<Cat>>(ex, "GetCatsInArea");
        }
    }

    /// <summary>
    /// Ottiene un singolo gatto per ID
    /// </summary>
    public async Task<ApiResponse<Cat>> GetCatByIdAsync(Guid id)
    {
        try
        {
            LogOperationStart("GetCatById", new { id });

            var pathParams = new Dictionary<string, object> { { "id", id } };
            var endpoint = BuildEndpoint(AppSettings.Api.Endpoints.CatsById, pathParams);

            var response = await GetAsync<Cat>(endpoint, "GetCatById API");

            if (response.Success && response.Data != null)
            {
                Logger?.LogInformation("✅ Caricato gatto: {CatName} (ID: {CatId})",
                    response.Data.Name, response.Data.Id);
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ Errore durante caricamento gatto con ID: {CatId}", id);
            return await ExceptionHandler.HandleExceptionAsync<Cat>(ex, "GetCatById");
        }
    }

    /// <summary>
    /// Crea un nuovo avvistamento di gatto
    /// </summary>
    public async Task<ApiResponse<Cat>> CreateCatAsync(Cat cat)
    {
        try
        {
            LogOperationStart("CreateCat", new { cat.Name, cat.Color, cat.Status });

            // Verifica autenticazione
            if (!_authService.IsAuthenticated)
            {
                return ApiResponse<Cat>.AuthErrorResponse("Devi essere autenticato per segnalare un gatto");
            }

            // Validazione dati
            var validationError = ValidateCatData(cat);
            if (validationError != null)
            {
                return validationError;
            }

            // Prepara dati per invio (rimuovi campi calcolati dal server)
            var catRequest = PrepareCreateCatRequest(cat);

            var endpoint = AppSettings.Api.Endpoints.CatsBase;
            var response = await PostAsync<Cat>(endpoint, catRequest, "CreateCat API");

            if (response.Success && response.Data != null)
            {
                Logger?.LogInformation("✅ Gatto creato: {CatName} (ID: {CatId}) da utente: {Username}",
                    response.Data.Name, response.Data.Id, _authService.CurrentUser?.Username);
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ Errore durante creazione gatto: {CatName}", cat.Name);
            return await ExceptionHandler.HandleExceptionAsync<Cat>(ex, "CreateCat");
        }
    }

    /// <summary>
    /// Aggiorna un gatto esistente
    /// </summary>
    public async Task<ApiResponse<Cat>> UpdateCatAsync(Guid id, Cat updatedCat)
    {
        try
        {
            LogOperationStart("UpdateCat", new { id, updatedCat.Name });

            // Verifica autenticazione
            if (!_authService.IsAuthenticated)
            {
                return ApiResponse<Cat>.AuthErrorResponse("Devi essere autenticato per modificare un gatto");
            }

            // Validazione dati
            var validationError = ValidateCatData(updatedCat);
            if (validationError != null)
            {
                return validationError;
            }

            var pathParams = new Dictionary<string, object> { { "id", id } };
            var endpoint = BuildEndpoint(AppSettings.Api.Endpoints.CatsById, pathParams);

            var response = await PutAsync<Cat>(endpoint, updatedCat, "UpdateCat API");

            if (response.Success && response.Data != null)
            {
                Logger?.LogInformation("✅ Gatto aggiornato: {CatName} (ID: {CatId})",
                    response.Data.Name, response.Data.Id);
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ Errore durante aggiornamento gatto con ID: {CatId}", id);
            return await ExceptionHandler.HandleExceptionAsync<Cat>(ex, "UpdateCat");
        }
    }

    /// <summary>
    /// Elimina un gatto
    /// </summary>
    public async Task<ApiResponse<bool>> DeleteCatAsync(Guid id)
    {
        try
        {
            LogOperationStart("DeleteCat", new { id });

            // Verifica autenticazione
            if (!_authService.IsAuthenticated)
            {
                return ApiResponse<bool>.AuthErrorResponse("Devi essere autenticato per eliminare un gatto")
                    .ConvertTo<bool>();
            }

            var pathParams = new Dictionary<string, object> { { "id", id } };
            var endpoint = BuildEndpoint(AppSettings.Api.Endpoints.CatsById, pathParams);

            var response = await DeleteAsync(endpoint, "DeleteCat API");

            if (response.Success)
            {
                Logger?.LogInformation("✅ Gatto eliminato con ID: {CatId}", id);
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ Errore durante eliminazione gatto con ID: {CatId}", id);
            var errorResponse = await ExceptionHandler.HandleExceptionAsync<object>(ex, "DeleteCat");
            return errorResponse.ConvertTo<bool>();
        }
    }

    /// <summary>
    /// Cerca gatti per nome o colore
    /// </summary>
    public async Task<ApiResponse<List<Cat>>> SearchCatsAsync(string searchTerm)
    {
        try
        {
            LogOperationStart("SearchCats", new { searchTerm });

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                // Se il termine è vuoto, ritorna tutti i gatti
                return await GetAllCatsAsync();
            }

            var queryParams = new Dictionary<string, object?>
            {
                { "q", searchTerm.Trim() }
            };

            var endpoint = AppSettings.Api.Endpoints.CatsSearch;
            var response = await GetAsync<List<Cat>>(endpoint, queryParams, "SearchCats API");

            if (response.Success && response.Data != null)
            {
                Logger?.LogInformation("✅ Trovati {Count} gatti per ricerca: '{SearchTerm}'",
                    response.Data.Count, searchTerm);
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ Errore durante ricerca gatti con termine: {SearchTerm}", searchTerm);
            return await ExceptionHandler.HandleExceptionAsync<List<Cat>>(ex, "SearchCats");
        }
    }

    /// <summary>
    /// Ottiene i commenti di un gatto
    /// </summary>
    public async Task<ApiResponse<List<Comment>>> GetCommentsAsync(Guid catId)
    {
        try
        {
            LogOperationStart("GetComments", new { catId });

            var pathParams = new Dictionary<string, object> { { "catId", catId } };
            var endpoint = BuildEndpoint(AppSettings.Api.Endpoints.Comments, pathParams);

            var response = await GetAsync<List<Comment>>(endpoint, "GetComments API");

            if (response.Success && response.Data != null)
            {
                Logger?.LogInformation("✅ Caricati {Count} commenti per gatto ID: {CatId}",
                    response.Data.Count, catId);
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ Errore durante caricamento commenti per gatto ID: {CatId}", catId);
            return await ExceptionHandler.HandleExceptionAsync<List<Comment>>(ex, "GetComments");
        }
    }

    /// <summary>
    /// Aggiunge un commento a un gatto
    /// </summary>
    public async Task<ApiResponse<Comment>> AddCommentAsync(Guid catId, string text)
    {
        try
        {
            LogOperationStart("AddComment", new { catId, textLength = text?.Length ?? 0 });

            // Verifica autenticazione
            if (!_authService.IsAuthenticated)
            {
                return ApiResponse<Comment>.AuthErrorResponse("Devi essere autenticato per commentare");
            }

            // Validazione
            if (string.IsNullOrWhiteSpace(text))
            {
                return ApiResponse<Comment>.ErrorResponse(
                    "Il commento non può essere vuoto",
                    "Testo obbligatorio"
                );
            }

            if (text.Length > 500)
            {
                return ApiResponse<Comment>.ErrorResponse(
                    "Il commento è troppo lungo",
                    "Massimo 500 caratteri consentiti"
                );
            }

            // Prepara richiesta
            var commentRequest = new
            {
                text = text.Trim(),
                catId = catId
            };

            var pathParams = new Dictionary<string, object> { { "catId", catId } };
            var endpoint = BuildEndpoint(AppSettings.Api.Endpoints.Comments, pathParams);

            var response = await PostAsync<Comment>(endpoint, commentRequest, "AddComment API");

            if (response.Success && response.Data != null)
            {
                Logger?.LogInformation("✅ Commento aggiunto da {Username} per gatto ID: {CatId}",
                    _authService.CurrentUser?.Username, catId);
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "❌ Errore durante aggiunta commento per gatto ID: {CatId}", catId);
            return await ExceptionHandler.HandleExceptionAsync<Comment>(ex, "AddComment");
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Valida i dati del gatto prima dell'invio
    /// </summary>
    private ApiResponse<Cat>? ValidateCatData(Cat cat)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(cat.Name))
        {
            errors.Add("Il nome del gatto è obbligatorio");
        }
        else if (cat.Name.Length > 50)
        {
            errors.Add("Il nome non può superare 50 caratteri");
        }

        if (string.IsNullOrWhiteSpace(cat.Color))
        {
            errors.Add("Il colore del gatto è obbligatorio");
        }

        if (cat.Description != null && cat.Description.Length > 500)
        {
            errors.Add("La descrizione non può superare 500 caratteri");
        }

        // Validazione posizione
        if (cat.Location.Latitude == 0 && cat.Location.Longitude == 0)
        {
            errors.Add("La posizione del gatto è obbligatoria");
        }

        if (Math.Abs(cat.Location.Latitude) > 90)
        {
            errors.Add("Latitudine non valida (deve essere tra -90 e 90)");
        }

        if (Math.Abs(cat.Location.Longitude) > 180)
        {
            errors.Add("Longitudine non valida (deve essere tra -180 e 180)");
        }

        if (errors.Any())
        {
            return ApiResponse<Cat>.ErrorResponse(
                "Dati del gatto non validi",
                errors
            );
        }

        return null;
    }

    /// <summary>
    /// Prepara i dati del gatto per la creazione (rimuove campi gestiti dal server)
    /// </summary>
    private object PrepareCreateCatRequest(Cat cat)
    {
        return new
        {
            name = cat.Name,
            description = cat.Description ?? "",
            color = cat.Color,
            status = cat.Status.ToString(),
            photoUrl = cat.PhotoUrl ?? "",
            location = new
            {
                type = "Point",
                coordinates = new[] { cat.Location.Longitude, cat.Location.Latitude },
                address = cat.Location.Address ?? "",
                city = cat.Location.City ?? "",
                postalCode = cat.Location.PostalCode ?? ""
            }
        };
    }

    #endregion
}