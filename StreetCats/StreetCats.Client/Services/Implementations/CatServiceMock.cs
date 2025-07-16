using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreetCats.Client.Models;
using StreetCats.Client.Models.DTOs;
using StreetCats.Client.Services.Interfaces;
using StreetCats.Client.Services.Auth.Interfaces;
using StreetCats.Client.Services.Exceptions.Interfaces;
using StreetCats.Client.Services.Configuration.Interfaces;

namespace StreetCats.Client.Services.Implementations;

/// <summary>
/// Implementazione MOCK del servizio gatti per sviluppo
/// Contiene dati realistici di gatti nell'area di Napoli
/// Simula operazioni CRUD senza backend reale
/// </summary>
public class CatServiceMock : ICatService
{
    private readonly IMapService _mapService;
    private readonly IAuthService _authService;
    private List<Cat> _mockCats;
    private List<Comment> _mockComments;

    public CatServiceMock(IMapService mapService, IAuthService authService)
    {
        _mapService = mapService;
        _authService = authService;
        _mockCats = GenerateMockCats();
        _mockComments = GenerateMockComments();
    }

    #region Public API Methods

    /// <summary>
    /// Ottiene tutti i gatti (con simulazione delay di rete)
    /// </summary>
    public async Task<ApiResponse<List<Cat>>> GetAllCatsAsync()
    {
        await SimulateNetworkDelay();

        // Aggiungi commenti ai gatti
        foreach (var cat in _mockCats)
        {
            cat.Comments = _mockComments.Where(c => c.CatId == cat.Id).ToList();
        }

        return new ApiResponse<List<Cat>>
        {
            Success = true,
            Message = "Gatti caricati con successo",
            Data = _mockCats.ToList() // ToList() per creare copia
        };
    }

    /// <summary>
    /// Ricerca gatti in un'area geografica specifica
    /// Usa il MapService per calcoli di distanza
    /// </summary>
    public async Task<ApiResponse<List<Cat>>> GetCatsInAreaAsync(double latitude, double longitude, double radiusKm = 5.0)
    {
        await SimulateNetworkDelay();

        var catsInArea = _mockCats.Where(cat =>
            _mapService.IsPointInRadius(
                latitude, longitude,
                cat.Location.Latitude, cat.Location.Longitude,
                radiusKm
            )).ToList();

        // Aggiungi commenti
        foreach (var cat in catsInArea)
        {
            cat.Comments = _mockComments.Where(c => c.CatId == cat.Id).ToList();
        }

        return new ApiResponse<List<Cat>>
        {
            Success = true,
            Message = $"Trovati {catsInArea.Count} gatti nel raggio di {radiusKm} km",
            Data = catsInArea
        };
    }

    /// <summary>
    /// Ottiene un singolo gatto per ID
    /// </summary>
    public async Task<ApiResponse<Cat>> GetCatByIdAsync(Guid id)
    {
        await SimulateNetworkDelay();

        var cat = _mockCats.FirstOrDefault(c => c.Id == id);
        if (cat == null)
        {
            return new ApiResponse<Cat>
            {
                Success = false,
                Message = "Gatto non trovato",
                Errors = new List<string> { $"Nessun gatto con ID {id}" }
            };
        }

        // Aggiungi commenti
        cat.Comments = _mockComments.Where(c => c.CatId == cat.Id).ToList();

        return new ApiResponse<Cat>
        {
            Success = true,
            Message = "Gatto trovato",
            Data = cat
        };
    }

    /// <summary>
    /// Crea un nuovo avvistamento di gatto
    /// Richiede utente autenticato
    /// </summary>
    public async Task<ApiResponse<Cat>> CreateCatAsync(Cat cat)
    {
        await SimulateNetworkDelay(1500); // Operazione più lenta

        if (!_authService.IsAuthenticated)
        {
            return new ApiResponse<Cat>
            {
                Success = false,
                Message = "Devi essere autenticato per segnalare un gatto",
                Errors = new List<string> { "Accesso negato" }
            };
        }

        // Validazioni
        if (string.IsNullOrWhiteSpace(cat.Name))
        {
            return new ApiResponse<Cat>
            {
                Success = false,
                Message = "Il nome del gatto è obbligatorio",
                Errors = new List<string> { "Nome mancante" }
            };
        }

        // Imposta dati del gatto
        cat.Id = Guid.NewGuid();
        cat.CreatedAt = DateTime.UtcNow;
        cat.LastSeen = DateTime.UtcNow;
        cat.CreatedBy = _authService.CurrentUser!.Id.ToString();
        cat.CreatedByName = _authService.CurrentUser!.Username;
        cat.Comments = new List<Comment>();

        // Aggiungi alla lista mock
        _mockCats.Add(cat);

        return new ApiResponse<Cat>
        {
            Success = true,
            Message = "Gatto segnalato con successo!",
            Data = cat
        };
    }

    /// <summary>
    /// Aggiorna un gatto esistente (solo se sei il creatore)
    /// </summary>
    public async Task<ApiResponse<Cat>> UpdateCatAsync(Guid id, Cat updatedCat)
    {
        await SimulateNetworkDelay();

        if (!_authService.IsAuthenticated)
        {
            return new ApiResponse<Cat>
            {
                Success = false,
                Message = "Devi essere autenticato",
                Errors = new List<string> { "Accesso negato" }
            };
        }

        var existingCat = _mockCats.FirstOrDefault(c => c.Id == id);
        if (existingCat == null)
        {
            return new ApiResponse<Cat>
            {
                Success = false,
                Message = "Gatto non trovato",
                Errors = new List<string> { "ID non valido" }
            };
        }

        // Verifica che l'utente sia il creatore
        if (existingCat.CreatedBy != _authService.CurrentUser!.Id.ToString())
        {
            return new ApiResponse<Cat>
            {
                Success = false,
                Message = "Puoi modificare solo i gatti che hai segnalato tu",
                Errors = new List<string> { "Operazione non autorizzata" }
            };
        }

        // Aggiorna dati (mantieni alcuni campi originali)
        existingCat.Name = updatedCat.Name;
        existingCat.Description = updatedCat.Description;
        existingCat.Color = updatedCat.Color;
        existingCat.Status = updatedCat.Status;
        existingCat.PhotoUrl = updatedCat.PhotoUrl;
        existingCat.Location = updatedCat.Location;
        existingCat.LastSeen = DateTime.UtcNow;

        return new ApiResponse<Cat>
        {
            Success = true,
            Message = "Gatto aggiornato con successo",
            Data = existingCat
        };
    }

    /// <summary>
    /// Elimina un gatto (solo se sei il creatore)
    /// </summary>
    public async Task<ApiResponse<bool>> DeleteCatAsync(Guid id)
    {
        await SimulateNetworkDelay();

        if (!_authService.IsAuthenticated)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Devi essere autenticato",
                Data = false
            };
        }

        var cat = _mockCats.FirstOrDefault(c => c.Id == id);
        if (cat == null)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Gatto non trovato",
                Data = false
            };
        }

        if (cat.CreatedBy != _authService.CurrentUser!.Id.ToString())
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Puoi eliminare solo i gatti che hai segnalato tu",
                Data = false
            };
        }

        // Rimuovi gatto e i suoi commenti
        _mockCats.Remove(cat);
        _mockComments.RemoveAll(c => c.CatId == id);

        return new ApiResponse<bool>
        {
            Success = true,
            Message = "Gatto eliminato con successo",
            Data = true
        };
    }

    /// <summary>
    /// Cerca gatti per nome o colore
    /// </summary>
    public async Task<ApiResponse<List<Cat>>> SearchCatsAsync(string searchTerm)
    {
        await SimulateNetworkDelay();

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllCatsAsync();
        }

        var searchLower = searchTerm.ToLower();
        var results = _mockCats.Where(cat =>
            cat.Name.ToLower().Contains(searchLower) ||
            cat.Color.ToLower().Contains(searchLower) ||
            cat.Description.ToLower().Contains(searchLower) ||
            cat.Location.Address.ToLower().Contains(searchLower)
        ).ToList();

        // Aggiungi commenti
        foreach (var cat in results)
        {
            cat.Comments = _mockComments.Where(c => c.CatId == cat.Id).ToList();
        }

        return new ApiResponse<List<Cat>>
        {
            Success = true,
            Message = $"Trovati {results.Count} gatti per '{searchTerm}'",
            Data = results
        };
    }

    /// <summary>
    /// Ottiene i commenti di un gatto
    /// </summary>
    public async Task<ApiResponse<List<Comment>>> GetCommentsAsync(Guid catId)
    {
        await SimulateNetworkDelay();

        var comments = _mockComments
            .Where(c => c.CatId == catId)
            .OrderBy(c => c.CreatedAt)
            .ToList();

        return new ApiResponse<List<Comment>>
        {
            Success = true,
            Message = $"Trovati {comments.Count} commenti",
            Data = comments
        };
    }

    /// <summary>
    /// Aggiunge un commento a un gatto
    /// </summary>
    public async Task<ApiResponse<Comment>> AddCommentAsync(Guid catId, string text)
    {
        await SimulateNetworkDelay(800);

        if (!_authService.IsAuthenticated)
        {
            return new ApiResponse<Comment>
            {
                Success = false,
                Message = "Devi essere autenticato per commentare"
            };
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new ApiResponse<Comment>
            {
                Success = false,
                Message = "Il commento non può essere vuoto",
                Errors = new List<string> { "Testo obbligatorio" }
            };
        }

        var cat = _mockCats.FirstOrDefault(c => c.Id == catId);
        if (cat == null)
        {
            return new ApiResponse<Comment>
            {
                Success = false,
                Message = "Gatto non trovato"
            };
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            CatId = catId,
            UserId = new Guid(_authService.CurrentUser!.Id),
            UserName = _authService.CurrentUser!.Username,
            Text = text,
            CreatedAt = DateTime.UtcNow
        };

        _mockComments.Add(comment);

        return new ApiResponse<Comment>
        {
            Success = true,
            Message = "Commento aggiunto con successo",
            Data = comment
        };
    }

    #endregion

    #region Data Generation Methods

    /// <summary>
    /// Genera lista di gatti mock realistici per Napoli
    /// </summary>
    private List<Cat> GenerateMockCats()
    {
        var cats = new List<Cat>();

        // 🎯 ZONE FAMOSE DI NAPOLI con coordinate reali
        var napoliLocations = new[]
        {
            new { Name = "Spaccanapoli", Lat = 40.8467, Lng = 14.2539, Address = "Spaccanapoli, 80138 Napoli NA" },
            new { Name = "Quartieri Spagnoli", Lat = 40.8398, Lng = 14.2463, Address = "Quartieri Spagnoli, 80132 Napoli NA" },
            new { Name = "Vomero", Lat = 40.8538, Lng = 14.2285, Address = "Via Scarlatti, 80129 Napoli NA" },
            new { Name = "Chiaia", Lat = 40.8260, Lng = 14.2466, Address = "Via Chiaia, 80121 Napoli NA" },
            new { Name = "Sanità", Lat = 40.8583, Lng = 14.2614, Address = "Rione Sanità, 80136 Napoli NA" },
            new { Name = "Posillipo", Lat = 40.8096, Lng = 14.2034, Address = "Via Posillipo, 80123 Napoli NA" },
            new { Name = "Centro Storico", Lat = 40.8518, Lng = 14.2681, Address = "Piazza del Gesù, 80134 Napoli NA" },
            new { Name = "Mergellina", Lat = 40.8245, Lng = 14.2087, Address = "Via Mergellina, 80122 Napoli NA" }
        };

        // 🐱 NOMI TIPICI NAPOLETANI
        var catNames = new[]
        {
            "Pulcinella", "Sofì", "Gennaro", "Carmela", "Totò", "Peppino", "Lucia", "Salvatore",
            "Antò", "Giggino", "Pina", "Ciro", "Assunta", "Mimmo", "Concetta", "Pasquale",
            "Nunzia", "Raffaele", "Filomena", "Eduardo", "Rosaria", "Vincenzo", "Giuseppina"
        };

        var colors = new[] { "Arancione", "Grigio", "Nero", "Bianco", "Tigrato", "Maculato", "Rosso", "Tricolore" };
        var statuses = new[] { CatStatus.Avvistato, CatStatus.InCura, CatStatus.Adottato };

        var descriptions = new[]
        {
            "Gatto molto affettuoso, si avvicina alle persone",
            "Timido ma curioso, si nasconde facilmente",
            "Sempre in cerca di cibo, molto socievole",
            "Gatto giovane, molto giocherellone",
            "Anziano e tranquillo, ama dormire al sole",
            "Mamma gatta con cuccioli nelle vicinanze",
            "Gatto ferito, ha bisogno di cure veterinarie",
            "Molto territoriale, conosce bene la zona",
            "Si lascia accarezzare, probabilmente era domestico",
            "Gatto selvatico, mantiene le distanze"
        };

        // Genera 25 gatti casuali
        var random = new Random(42); // Seed fisso per risultati consistenti
        for (int i = 0; i < 25; i++)
        {
            var location = napoliLocations[random.Next(napoliLocations.Length)];

            // Aggiungi variazione casuale alle coordinate (±200 metri)
            var latVariation = (random.NextDouble() - 0.5) * 0.004; // ~200m
            var lngVariation = (random.NextDouble() - 0.5) * 0.004;

            var cat = new Cat
            {
                Id = Guid.NewGuid(),
                Name = catNames[random.Next(catNames.Length)],
                Description = descriptions[random.Next(descriptions.Length)],
                Color = colors[random.Next(colors.Length)],
                Status = statuses[random.Next(statuses.Length)],
                PhotoUrl = $"https://placekitten.com/{200 + i}/{200 + i}", // Placeholder immagini
                Location = new Location
                {
                    Latitude = location.Lat + latVariation,
                    Longitude = location.Lng + lngVariation,
                    Address = location.Address,
                    City = "Napoli",
                    PostalCode = GetPostalCodeForArea(location.Name)
                },
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 90)), // Ultimi 3 mesi
                LastSeen = DateTime.UtcNow.AddDays(-random.Next(0, 7)),   // Ultima settimana
                CreatedBy = "system",
                CreatedByName = GetRandomUserName(random),
                Comments = new List<Comment>()
            };

            cats.Add(cat);
        }

        return cats;
    }

    /// <summary>
    /// Genera commenti mock realistici
    /// </summary>
    private List<Comment> GenerateMockComments()
    {
        var comments = new List<Comment>();
        var random = new Random(43);

        var commentTexts = new[]
        {
            "L'ho visto anche io stamattina! Stava cercando cibo vicino al bar.",
            "È sempre qui in questa zona, lo conosco da mesi.",
            "Sembra stare bene, ha il pelo lucido e pulito.",
            "Credo sia scappato da casa, non sembra un randagio.",
            "Ha bisogno di cure, ho notato che zoppica un po'.",
            "È molto dolce, si è fatto accarezzare senza problemi.",
            "Lo vedo spesso con altri due gatti, forse sono una famiglia.",
            "Qualcuno dovrebbe portarlo dal veterinario.",
            "È diventato molto magro nell'ultimo periodo.",
            "Ieri l'ho visto dormire nella chiesa, è al sicuro lì.",
            "I negozianti della zona gli danno sempre qualcosa da mangiare.",
            "Sembra che abbia trovato casa, non lo vedo più in giro!",
            "È aggressivo con gli altri gatti, state attenti.",
            "Ho lasciato delle crocchette nel solito posto.",
            "Perfetto per essere adottato, è molto socievole!"
        };

        var userNames = new[]
        {
            "MariaNapoli", "AntonioVomero", "LuciaChiaia", "SalvatoreSpaccaP",
            "CarmineDelCentro", "RosariaSanita", "VincenzoMergellina", "AssuntaQuartieri"
        };

        // Aggiungi 1-4 commenti casuali a ogni gatto
        foreach (var cat in _mockCats ?? new List<Cat>())
        {
            var numComments = random.Next(0, 5); // 0-4 commenti per gatto

            for (int i = 0; i < numComments; i++)
            {
                var comment = new Comment
                {
                    Id = Guid.NewGuid(),
                    CatId = cat.Id,
                    UserId = Guid.NewGuid(),
                    UserName = userNames[random.Next(userNames.Length)],
                    Text = commentTexts[random.Next(commentTexts.Length)],
                    CreatedAt = cat.CreatedAt.AddDays(random.Next(1, 30))
                };

                comments.Add(comment);
            }
        }

        return comments;
    }

    #endregion

    #region Helper Methods

    private async Task SimulateNetworkDelay(int maxMs = 800)
    {
        // Simula latenza di rete realistica
        var delay = Random.Shared.Next(200, maxMs);
        await Task.Delay(delay);
    }

    private string GetPostalCodeForArea(string areaName)
    {
        return areaName switch
        {
            "Spaccanapoli" => "80138",
            "Quartieri Spagnoli" => "80132",
            "Vomero" => "80129",
            "Chiaia" => "80121",
            "Sanità" => "80136",
            "Posillipo" => "80123",
            "Centro Storico" => "80134",
            "Mergellina" => "80122",
            _ => "80100"
        };
    }

    private string GetRandomUserName(Random random)
    {
        var names = new[]
        {
            "Giuseppe82", "Maria_Napoli", "Antonio.V", "Lucia_C", "Ciro2024",
            "Carmela_Gatti", "Peppe_Centro", "Rosa_Chiaia", "Gennaro_Vomero"
        };
        return names[random.Next(names.Length)];
    }

    #endregion
}