namespace Valora.Application.Exceptions;

public sealed class InactiveOrganizationException() : Exception(
    "A conta da empresa está bloqueada ou inativa. Fale com o administrador ou com o suporte Valora.");
