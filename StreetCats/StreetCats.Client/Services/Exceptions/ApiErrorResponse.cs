using System.Collections.Generic;

namespace StreetCats.Client.Services.Exceptions;

/// <summary>
/// Modello per deserializzare risposte di errore strutturate dal server
/// </summary>
internal class ApiErrorResponse
{
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public string? Type { get; set; }
    public string? TraceId { get; set; }
}