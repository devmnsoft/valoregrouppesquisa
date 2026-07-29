using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface ILegacyDataNormalizer { string? NormalizeEmail(string? value); string? NormalizeDocument(string? value); string NormalizeStatus(string? value); string NormalizeRole(string? value); string NormalizeModule(string? value); DateTime? NormalizeDate(object? value); string MaskSensitiveJson(string? json); }
