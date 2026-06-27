using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.ReadModels;
namespace Valora.Application.Services;
public sealed class PublicResultValidator(IResponseRepository responses,IResultTokenService tokens)
{
    public async Task<ResponseReadModel> ValidateAsync(Guid responseId,PublicResultRequest request)
    {
        if (responseId == Guid.Empty) throw new InvalidOperationException("Resultado inválido.");
        if (string.IsNullOrWhiteSpace(request.ResultToken)) throw new UnauthorizedAccessException("Token do resultado obrigatório.");
        var response = await responses.GetByIdAsync(responseId) ?? throw new InvalidOperationException("Resultado não encontrado.");
        if (response.IsDeleted || response.Status == "deleted" || response.Status == "inactive") throw new InvalidOperationException("Resultado indisponível.");
        if (string.IsNullOrWhiteSpace(response.ResultTokenHash) || !tokens.Verify(request.ResultToken, response.ResultTokenHash)) throw new UnauthorizedAccessException("Token do resultado inválido.");
        return response;
    }
}
