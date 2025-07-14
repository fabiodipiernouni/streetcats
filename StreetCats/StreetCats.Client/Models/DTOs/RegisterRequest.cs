using System.ComponentModel.DataAnnotations;

namespace StreetCats.Client.Models.DTOs;

/// <summary>
/// DTO per la richiesta di registrazione
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Il nome utente è obbligatorio")]
    [StringLength(50, ErrorMessage = "Il nome utente non può superare 50 caratteri")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'email è obbligatoria")]
    [EmailAddress(ErrorMessage = "Formato email non valido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La password è obbligatoria")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La password deve essere tra 6 e 100 caratteri")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La conferma password è obbligatoria")]
    [Compare("Password", ErrorMessage = "Le password non coincidono")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Il nome completo non può superare 100 caratteri")]
    public string FullName { get; set; } = string.Empty;
}