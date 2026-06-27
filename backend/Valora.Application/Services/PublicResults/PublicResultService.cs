using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class PublicResultService(PublicResultValidator validator, ISurveyRepository surveys, IResultRepository results, ICertificateRepository certificates, PublicResultAssembler assembler, ILogger<PublicResultService> logger) : IPublicResultService
{
    public async Task<PublicResultResponse> GetAsync(Guid responseId, PublicResultRequest request)
    {
        try
        {
            logger.LogInformation("Public result lookup started. ResponseId={ResponseId} HasToken={HasToken}", responseId, !string.IsNullOrWhiteSpace(request.ResultToken));
            var response = await validator.ValidateAsync(responseId, request);
            var survey = await surveys.GetActivePublicSurveyAsync(response.SurveyId) ?? throw new InvalidOperationException("Pesquisa do resultado não encontrada.");
            var score = await results.GetByResponseAsync(responseId) ?? throw new InvalidOperationException("Pontuação não encontrada.");
            var dims = await results.GetDimensionsByResponseIdAsync(responseId);
            logger.LogInformation("Public result found. ResponseId={ResponseId} SurveyId={SurveyId} DimensionCount={DimensionCount}", responseId, response.SurveyId, dims.Count);
            var cert = await certificates.GetByResponseAsync(responseId) ?? throw new InvalidOperationException("Certificado não encontrado.");
            logger.LogInformation("Public result certificate found. ResponseId={ResponseId}", responseId);
            return assembler.Assemble(response, survey, score, dims, cert);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Public result token validation failed. ResponseId={ResponseId}", responseId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Public result lookup failed. ResponseId={ResponseId}", responseId);
            throw;
        }
    }
}
