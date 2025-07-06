using System;
using System.Collections.Generic;

namespace StreetCats.Client.Models.DTOs;

/// <summary>
/// Wrapper generico per le risposte delle API
/// </summary>
/// <typeparam name="T">Tipo dei dati restituiti</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Response per autenticazione con token JWT
/// </summary>
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public User User { get; set; } = new();
}