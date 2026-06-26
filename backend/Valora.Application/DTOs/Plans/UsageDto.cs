namespace Valora.Application.DTOs;

public record UsageDto(int ActiveSurveys,int ResponsesThisMonth,int Managers,Dictionary<string,int> Limits);
