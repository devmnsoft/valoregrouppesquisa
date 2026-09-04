namespace Valora.Web.Models.ViewModels;
public sealed record ExecutiveMetricCardModel(string Label,string Value,string Help,string Tone,string Url);
public sealed record EmptyDashboardStateModel(string Title,string Description,string Url,string Action);
public sealed record NextActionCardModel(string Title,string Description,string Url,string Action);
