using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Application.Services;
using ValoraPesquisa.Domain.Entities;
using ValoraPesquisa.Domain.Enums;
namespace ValoraPesquisa.Api.Controllers;
public abstract class ApiBase:ControllerBase{ protected CurrentUser Current()=>new(Guid.Parse(User.FindFirstValue("user_id")!),Guid.TryParse(User.FindFirstValue("organization_id"),out var o)?o:null,User.FindFirstValue("role")!,User.FindFirstValue("email")!); protected string Cid=>HttpContext.Items["CorrelationId"]?.ToString()??""; protected Guid OrgOrThrow(CurrentUser u,Guid? input=null)=>u.IsAdminValora?(input??throw new ArgumentException("organizationId obrigatório.")):(u.OrganizationId??throw new UnauthorizedAccessException("Organização não definida.")); }
