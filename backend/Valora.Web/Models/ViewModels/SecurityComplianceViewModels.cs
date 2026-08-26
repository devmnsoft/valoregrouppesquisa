using System.ComponentModel.DataAnnotations;
namespace Valora.Web.Models.ViewModels;
public sealed record SecurityMetricViewModel(string Label,int Value,string Tone="neutral");
public sealed record SecurityCompliancePageViewModel(string Title,string Description,IReadOnlyList<SecurityMetricViewModel> Metrics,IReadOnlyList<string> Guidance);
public sealed class ConsentInputViewModel { [Required] public string ConsentType {get;set;}="privacy_policy"; [Required] public string ConsentVersion {get;set;}="1"; [Required] public string Subject {get;set;}="Usuário autenticado"; }
public sealed class DataRequestInputViewModel { [Required,EmailAddress] public string RequesterEmail {get;set;}=""; [Required] public string RequestType {get;set;}="access"; [Required,StringLength(2000)] public string Description {get;set;}=""; }
public sealed class RetentionInputViewModel { [Required] public string DataCategory {get;set;}="responses"; [Range(1,3650)] public int RetentionDays {get;set;}=365; [Required] public string ExpirationAction {get;set;}="anonymize"; }
public sealed class IncidentInputViewModel { [Required] public string Severity {get;set;}="medium"; [Required] public string IncidentType {get;set;}="suspected_access"; [Required] public string Title {get;set;}=""; [Required] public string Description {get;set;}=""; [Required] public string EvidenceSummary {get;set;}=""; }
