namespace Valora.Application.Exceptions;

/// <summary>Indicates that required system-owned configuration or seed data is absent or inconsistent.</summary>
public sealed class ApplicationConfigurationException(string message) : Exception(message);
