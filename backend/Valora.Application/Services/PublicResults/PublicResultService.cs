using Valora.Application.Contracts;
using Valora.Application.DTOs;
namespace Valora.Application.Services;
public sealed class PublicResultService(PublicResultValidator validator,ISurveyRepository surveys,IResultRepository results,ICertificateRepository certificates,PublicResultAssembler assembler):IPublicResultService
{
    public async Task<PublicResultResponse> GetAsync(Guid responseId, PublicResultRequest request)
    {
        var response = await validator.ValidateAsync(responseId, request);
        var survey = await surveys.GetActivePublicSurveyAsync(response.SurveyId) ?? throw new InvalidOperationException("Pesquisa do resultado não encontrada.");
        var score = await results.GetByResponseAsync(responseId) ?? throw new InvalidOperationException("Pontuação não encontrada.");
        var dims = await results.GetDimensionsByResponseIdAsync(responseId);
        var cert = await certificates.GetByResponseAsync(responseId) ?? throw new InvalidOperationException("Certificado não encontrado.");
        return assembler.Assemble(response, survey, score, dims, cert);
    }
}
