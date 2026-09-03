using System.Text.Json;
using Valora.Application.Enterprise;

namespace Valora.Tests;

public sealed class EnterpriseV6Tests
{
    [Fact]
    public async Task Companies_normalizes_optional_filters_and_pagination_before_querying()
    {
        var repository = new NoopRepository();
        var service = new EnterpriseService(repository, new NoopAudit());

        await service.CompaniesAsync(new("  Acme  ", " ACTIVE ", " Enterprise ", " HEALTHY ", null, null, 0, 500), CancellationToken.None);

        Assert.Equal(new EnterpriseListQuery("Acme", "active", "enterprise", "healthy", null, null, 1, 100), repository.LastCompanyQuery);
    }

    [Fact]
    public void Csv_preview_reports_invalid_rows_instead_of_silently_importing()
    {
        var service = new EnterpriseService(new NoopRepository(), new NoopAudit());
        var preview = service.PreviewCsv("members", "nome;email\nAna;ana@valora.com\n;invalido");
        Assert.Equal(1, preview.Valid);
        Assert.Equal(1, preview.Invalid);
        Assert.Equal(3, preview.Rows[1].Line);
        Assert.Contains("Nome é obrigatório.", preview.Rows[1].Errors);
        Assert.Contains("E-mail inválido.", preview.Rows[1].Errors);
        Assert.Equal(64, preview.ConfirmationToken.Length);
    }

    [Fact]
    public void Csv_preview_rejects_unknown_import_type()
    {
        var service = new EnterpriseService(new NoopRepository(), new NoopAudit());
        Assert.Throws<Valora.Application.Exceptions.ValidationAppException>(() => service.PreviewCsv("anything", "nome\nAna"));
    }

    private sealed class NoopAudit : Valora.Application.Contracts.IAuditRepository
    { public Task AddAsync(Valora.Application.DTOs.AuditEntry entry)=>Task.CompletedTask; public Task LogAsync(Valora.Application.DTOs.AuditEntry entry,System.Data.IDbTransaction? transaction=null)=>Task.CompletedTask; public Task<IReadOnlyList<dynamic>> ListAdminAsync(Guid organizationId,int limit=100)=>Task.FromResult<IReadOnlyList<dynamic>>([]); }
    private sealed class NoopRepository : IEnterpriseRepository
    {
        public EnterpriseListQuery? LastCompanyQuery { get; private set; }
        public Task<PortfolioSummary> SummaryAsync(CancellationToken ct)=>throw new NotImplementedException(); public Task<EnterprisePage<PortfolioCompany>> CompaniesAsync(EnterpriseListQuery q,CancellationToken ct){LastCompanyQuery=q;return Task.FromResult(new EnterprisePage<PortfolioCompany>([],0,q.Page,q.PageSize));} public Task<EnterprisePage<CrmLead>> LeadsAsync(EnterpriseListQuery q,CancellationToken ct)=>throw new NotImplementedException(); public Task<Guid> CreateLeadAsync(CrmLead l,CancellationToken ct)=>throw new NotImplementedException(); public Task UpdateCompanyStatusAsync(Guid id,string status,CancellationToken ct)=>throw new NotImplementedException(); public Task<IReadOnlyList<EnterpriseItem>> ListItemsAsync(Guid? o,string k,CancellationToken ct)=>throw new NotImplementedException(); public Task<Guid> UpsertItemAsync(Guid? o,Guid? id,UpsertEnterpriseItemRequest r,CancellationToken ct)=>throw new NotImplementedException(); public Task SetItemStatusAsync(Guid? o,Guid id,string s,CancellationToken ct)=>throw new NotImplementedException(); public Task<ApiKeyIssued> CreateApiKeyAsync(Guid o,string n,IReadOnlyList<string> s,string h,string p,string secret,CancellationToken ct)=>throw new NotImplementedException();
    }
}
