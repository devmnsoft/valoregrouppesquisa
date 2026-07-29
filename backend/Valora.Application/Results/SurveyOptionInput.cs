namespace Valora.Application.Results;

public sealed record SurveyOptionInput(string Id, decimal Score, bool Correct = false);
