using StreetCats.Client.Models;
using StreetCats.Client.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StreetCats.Client.Services.Interfaces;

/// <summary>
/// Servizio per la gestione dei gatti e degli avvistamenti
/// </summary>
public interface ICatService
{
    /// <summary>
    /// Ottiene tutti i gatti
    /// </summary>
    /// <returns>Lista di gatti</returns>
    Task<ApiResponse<List<Cat>>> GetAllCatsAsync();

    /// <summary>
    /// Ottiene i gatti in un'area geografica specifica
    /// </summary>
    /// <param name="latitude">Latitudine centro</param>
    /// <param name="longitude">Longitudine centro</param>
    /// <param name="radiusKm">Raggio in chilometri</param>
    /// <returns>Lista di gatti nell'area</returns>
    Task<ApiResponse<List<Cat>>> GetCatsInAreaAsync(double latitude, double longitude, double radiusKm = 5.0);

    /// <summary>
    /// Ottiene un gatto specifico per ID
    /// </summary>
    /// <param name="id">ID del gatto</param>
    /// <returns>Dati del gatto</returns>
    Task<ApiResponse<Cat>> GetCatByIdAsync(Guid id);

    /// <summary>
    /// Crea un nuovo avvistamento di gatto
    /// </summary>
    /// <param name="cat">Dati del gatto</param>
    /// <returns>Gatto creato</returns>
    Task<ApiResponse<Cat>> CreateCatAsync(Cat cat);

    /// <summary>
    /// Aggiorna i dati di un gatto
    /// </summary>
    /// <param name="id">ID del gatto</param>
    /// <param name="cat">Nuovi dati</param>
    /// <returns>Gatto aggiornato</returns>
    Task<ApiResponse<Cat>> UpdateCatAsync(Guid id, Cat cat);

    /// <summary>
    /// Elimina un gatto (solo se creato dall'utente corrente)
    /// </summary>
    /// <param name="id">ID del gatto</param>
    /// <returns>Risultato dell'operazione</returns>
    Task<ApiResponse<bool>> DeleteCatAsync(Guid id);

    /// <summary>
    /// Cerca gatti per nome o colore
    /// </summary>
    /// <param name="searchTerm">Termine di ricerca</param>
    /// <returns>Lista di gatti trovati</returns>
    Task<ApiResponse<List<Cat>>> SearchCatsAsync(string searchTerm);

    /// <summary>
    /// Ottiene i commenti di un gatto
    /// </summary>
    /// <param name="catId">ID del gatto</param>
    /// <returns>Lista di commenti</returns>
    Task<ApiResponse<List<Comment>>> GetCommentsAsync(Guid catId);

    /// <summary>
    /// Aggiunge un commento a un gatto
    /// </summary>
    /// <param name="catId">ID del gatto</param>
    /// <param name="text">Testo del commento</param>
    /// <returns>Commento creato</returns>
    Task<ApiResponse<Comment>> AddCommentAsync(Guid catId, string text);
}