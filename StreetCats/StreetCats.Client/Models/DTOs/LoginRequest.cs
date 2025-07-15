using System.ComponentModel.DataAnnotations;

namespace StreetCats.Client.Models.DTOs;

/// <summary>
/// DTO per la richiesta di login
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Il nome utente è obbligatorio")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La password è obbligatoria")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}