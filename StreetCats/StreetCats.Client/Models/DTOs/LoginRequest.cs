using System.ComponentModel.DataAnnotations;

namespace StreetCats.Client.Models.DTOs;

/// <summary>
/// DTO per la richiesta di login
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "L'email è obbligatoria")]
    [EmailAddress(ErrorMessage = "Formato email non valido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La password è obbligatoria")]
    public string Password { get; set; } = string.Empty;
}