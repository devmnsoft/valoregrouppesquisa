namespace Valora.Application.DTOs;

public record UsageDto(int ActiveSurveys,int ResponsesThisMonth,int Managers,Dictionary<string,int> Limits)
{
    // UsageRepository fills this dictionary with the complete monthly counter set.
    public IReadOnlyDictionary<string, int> Counters => Limits;
}
