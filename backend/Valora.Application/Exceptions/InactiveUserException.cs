namespace Valora.Application.Exceptions;

/// <summary>Indicates that the supplied credentials belong to a disabled account.</summary>
public sealed class InactiveUserException() : Exception(
    "Este usuário está inativo. Entre em contato com o administrador.");
