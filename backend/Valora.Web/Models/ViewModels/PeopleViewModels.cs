using System.ComponentModel.DataAnnotations;
using Valora.Application.People;
namespace Valora.Web.Models.ViewModels;
public sealed record PeopleViewModel(PeopleDashboardDto Dashboard,IReadOnlyList<PeopleProfileDto> Profiles,IReadOnlyList<PeopleTeamDto> Teams,string Section);
public sealed class PeopleProfileForm { [Required,StringLength(120,MinimumLength=2)] public string DisplayName{get;set;}=""; [StringLength(120)] public string? RoleTitle{get;set;} }
public sealed class PeopleTeamForm { [Required,StringLength(120,MinimumLength=2)] public string Name{get;set;}=""; [Required] public Guid ResponsibleProfileId{get;set;} }
