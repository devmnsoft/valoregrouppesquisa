namespace Valora.Application.Exceptions;

/// <summary>Indicates that valid credentials exist, but tenant access is incomplete.</summary>
public sealed class OrganizationAccessNotConfiguredException() : Exception(
    "Seu usuário ainda não possui acesso configurado para esta organização.");
