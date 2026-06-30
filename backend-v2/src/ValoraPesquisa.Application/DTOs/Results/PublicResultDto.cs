namespace ValoraPesquisa.Application.DTOs;
public record PublicResultDto(Guid ResponseId,Guid SurveyId,decimal TotalScore,decimal MaxScore,decimal Percentage,decimal Normalized5,string Level,string ResultJson,DateTimeOffset CreatedAt);
