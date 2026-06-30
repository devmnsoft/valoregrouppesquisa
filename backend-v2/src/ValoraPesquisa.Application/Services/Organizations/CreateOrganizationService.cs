using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Domain.Entities;
namespace ValoraPesquisa.Application.Services.Organizations;
public sealed class CreateOrganizationService(IOrganizationRepository repo){ public async Task<OrganizationDto> ExecuteAsync(Organization org){ if(string.IsNullOrWhiteSpace(org.Name)) throw new ArgumentException("Nome obrigatório."); if(!org.Email.Contains('@')) throw new ArgumentException("E-mail inválido."); return await repo.CreateAsync(org); } }
