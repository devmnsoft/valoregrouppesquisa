using System.Text.Json;
using Dapper;
using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Domain.Entities;
using ValoraPesquisa.Infrastructure.Database;

namespace ValoraPesquisa.Infrastructure.Repositories;
public static class Scope{ public static void DemandOrg(CurrentUser u,Guid org){ if(!u.IsAdminValora && u.OrganizationId!=org) throw new UnauthorizedAccessException("Acesso negado para esta organização."); } }
