using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Application.Services;
using ValoraPesquisa.Domain.Entities;
using ValoraPesquisa.Domain.Enums;

namespace ValoraPesquisa.Api.Controllers;
[ApiController,Authorize,Route("audit")] public sealed class AuditController(IAuditService audit):ApiBase{ [HttpGet("events")] public Task<IReadOnlyList<AuditEventDto>> Events()=>audit.ListAsync(Current()); }
