using System.ComponentModel.DataAnnotations;
using NewRich.Admin.Constants;
using NewRich.Constants.Messages;

namespace NewRich.Admin.Models;

public sealed class LoginViewModel
{
    [Display(Name = UiTexts.Usuario)]
    [Required(ErrorMessage = ValidationMessages.NombreUsuarioRequerido)]
    public string Usuario { get; set; } = string.Empty;

    [Display(Name = UiTexts.Contrasena)]
    [Required(ErrorMessage = ValidationMessages.PasswordRequerido)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? Error { get; set; }
}

public sealed class CambiarPasswordViewModel
{
    [Display(Name = UiTexts.ContrasenaActual)]
    [Required(ErrorMessage = ValidationMessages.PasswordRequerido)]
    [DataType(DataType.Password)]
    public string PasswordActual { get; set; } = string.Empty;

    [Display(Name = UiTexts.ContrasenaNueva)]
    [Required(ErrorMessage = ValidationMessages.PasswordRequerido)]
    [DataType(DataType.Password)]
    public string PasswordNuevo { get; set; } = string.Empty;

    [Display(Name = UiTexts.ContrasenaConfirmacion)]
    [Required(ErrorMessage = ValidationMessages.PasswordRequerido)]
    [DataType(DataType.Password)]
    public string PasswordConfirmacion { get; set; } = string.Empty;

    public string? Error { get; set; }
}
