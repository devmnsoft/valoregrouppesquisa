using System.Data;
using Valora.Application.Campaigns;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.ReadModels;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.Tests;

public sealed class CampaignsV10Tests {
    private readonly Guid _organizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _surveyId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task CreateAsync_WhenNameIsEmpty_ThrowsArgumentException() {
        var repo = new FakeCampaignRepository();
        var surveys = new FakeSurveyRepository();
        var service = new CampaignService(repo, surveys);
        var request = new CreateCampaignDto(_surveyId, "   ");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(_organizationId, _userId, request, CancellationToken.None));

        Assert.Contains("nome de campanha válido", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenSurveyNotFound_ThrowsArgumentException() {
        var repo = new FakeCampaignRepository();
        var surveys = new FakeSurveyRepository(); // Contains no surveys
        var service = new CampaignService(repo, surveys);
        var request = new CreateCampaignDto(_surveyId, "Campanha Teste");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(_organizationId, _userId, request, CancellationToken.None));

        Assert.Contains("Pesquisa não encontrada", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenDatesAreInverted_ThrowsArgumentException() {
        var repo = new FakeCampaignRepository();
        var surveys = new FakeSurveyRepository();
        surveys.AddSurvey(_organizationId, _surveyId, "Pesquisa Clima");

        var service = new CampaignService(repo, surveys);
        var now = DateTimeOffset.UtcNow;
        var request = new CreateCampaignDto(
            _surveyId,
            "Campanha Datas",
            StartsAt: now.AddDays(10),
            EndsAt: now.AddDays(5));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(_organizationId, _userId, request, CancellationToken.None));

        Assert.Contains("posterior à data de início", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_PersistsDraftAndReturnsDto() {
        var repo = new FakeCampaignRepository();
        var surveys = new FakeSurveyRepository();
        surveys.AddSurvey(_organizationId, _surveyId, "Pesquisa Clima");

        var service = new CampaignService(repo, surveys);
        var request = new CreateCampaignDto(_surveyId, "Campanha Válida", ResponseGoal: 100, Audience: "Geral");

        var result = await service.CreateAsync(_organizationId, _userId, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(CanonicalCampaignStatus.Draft, result.Status);
        Assert.Equal("Campanha Válida", result.Name);
        Assert.Equal(100, result.ResponseGoal);
        Assert.Equal("Geral", result.Audience);
    }

    [Fact]
    public async Task TransitionAsync_FromDraftToActive_Succeeds() {
        var repo = new FakeCampaignRepository();
        var surveys = new FakeSurveyRepository();
        surveys.AddSurvey(_organizationId, _surveyId, "Pesquisa");

        var service = new CampaignService(repo, surveys);
        var created = await service.CreateAsync(_organizationId, _userId, new CreateCampaignDto(_surveyId, "Campanha"), CancellationToken.None);

        var result = await service.TransitionAsync(_organizationId, created.Id, CanonicalCampaignStatus.Active, ct: CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(CanonicalCampaignStatus.Active, result.Status);
    }

    [Fact]
    public async Task TransitionAsync_FromDraftToClosed_ThrowsInvalidOperationException() {
        var repo = new FakeCampaignRepository();
        var surveys = new FakeSurveyRepository();
        surveys.AddSurvey(_organizationId, _surveyId, "Pesquisa");

        var service = new CampaignService(repo, surveys);
        var created = await service.CreateAsync(_organizationId, _userId, new CreateCampaignDto(_surveyId, "Campanha"), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.TransitionAsync(_organizationId, created.Id, CanonicalCampaignStatus.Closed, ct: CancellationToken.None));

        Assert.Contains("não é permitida", ex.Message);
    }

    [Fact]
    public async Task TransitionAsync_ToScheduledWithoutFutureDate_ThrowsInvalidOperationException() {
        var repo = new FakeCampaignRepository();
        var surveys = new FakeSurveyRepository();
        surveys.AddSurvey(_organizationId, _surveyId, "Pesquisa");

        var service = new CampaignService(repo, surveys);
        var created = await service.CreateAsync(_organizationId, _userId, new CreateCampaignDto(_surveyId, "Campanha"), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.TransitionAsync(_organizationId, created.Id, CanonicalCampaignStatus.Scheduled, ct: CancellationToken.None));

        Assert.Contains("Defina a data de início", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenCampaignExists_RemovesCampaign() {
        var repo = new FakeCampaignRepository();
        var surveys = new FakeSurveyRepository();
        surveys.AddSurvey(_organizationId, _surveyId, "Pesquisa");

        var service = new CampaignService(repo, surveys);
        var created = await service.CreateAsync(_organizationId, _userId, new CreateCampaignDto(_surveyId, "Campanha"), CancellationToken.None);

        var deleted = await service.DeleteAsync(_organizationId, created.Id, CancellationToken.None);
        Assert.True(deleted);

        var afterDelete = await service.GetByIdAsync(_organizationId, created.Id, CancellationToken.None);
        Assert.Null(afterDelete);
    }

    private sealed class FakeCampaignRepository : ICampaignRepository {
        private readonly Dictionary<Guid, CampaignDetailDto> _items = new();

        public Task<IReadOnlyList<CampaignSummaryDto>> ListAsync(Guid organizationId, CampaignFilter? filter, CancellationToken ct) {
            var list = _items.Values
                .Where(x => x.OrganizationId == organizationId)
                .Where(x => filter?.SurveyId is null || x.SurveyId == filter.SurveyId)
                .Where(x => filter?.UnitId is null || x.UnitId == filter.UnitId)
                .Where(x => filter?.DepartmentId is null || x.DepartmentId == filter.DepartmentId)
                .Where(x => string.IsNullOrWhiteSpace(filter?.Status) || x.Status.Equals(filter.Status, StringComparison.OrdinalIgnoreCase))
                .Select(x => new CampaignSummaryDto(
                    x.Id, x.OrganizationId, x.SurveyId, x.SurveyTitle, x.UnitId, x.UnitName,
                    x.DepartmentId, x.DepartmentName, x.Name, x.Audience, x.ResponseGoal,
                    x.ActualResponses, x.CompletionRate, x.StartsAt, x.EndsAt, x.Status,
                    x.PublicUrl, x.CreatedAt, x.UpdatedAt))
                .ToList();

            return Task.FromResult<IReadOnlyList<CampaignSummaryDto>>(list);
        }

        public Task<CampaignDetailDto?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken ct) {
            _items.TryGetValue(id, out var item);
            return Task.FromResult(item?.OrganizationId == organizationId ? item : null);
        }

        public Task<CampaignDetailDto> CreateAsync(Campaign campaign, CancellationToken ct) {
            var dto = new CampaignDetailDto(
                campaign.Id, campaign.OrganizationId, campaign.SurveyId, "Pesquisa Clima",
                campaign.UnitId, null, campaign.DepartmentId, null, campaign.Name,
                campaign.Audience, campaign.ResponseGoal, 0, 0m, campaign.StartsAt,
                campaign.EndsAt, campaign.Status, $"/p/s/{Guid.NewGuid():N}",
                campaign.CreatedBy, "Admin", campaign.CreatedAt, campaign.UpdatedAt);

            _items[campaign.Id] = dto;
            return Task.FromResult(dto);
        }

        public Task<CampaignDetailDto?> UpdateAsync(Campaign campaign, CancellationToken ct) {
            if (!_items.ContainsKey(campaign.Id)) return Task.FromResult<CampaignDetailDto?>(null);
            return Task.FromResult<CampaignDetailDto?>(new CampaignDetailDto(
                campaign.Id, campaign.OrganizationId, campaign.SurveyId, "Pesquisa Clima",
                campaign.UnitId, null, campaign.DepartmentId, null, campaign.Name,
                campaign.Audience, campaign.ResponseGoal, 0, 0m, campaign.StartsAt,
                campaign.EndsAt, campaign.Status, null,
                campaign.CreatedBy, "Admin", campaign.CreatedAt, DateTime.UtcNow));
        }

        public Task<bool> TransitionStatusAsync(
            Guid organizationId,
            Guid id,
            string expectedStatus,
            string newStatus,
            Guid? userId = null,
            string? justification = null,
            string? correlationId = null,
            CancellationToken ct = default) {
            if (!_items.TryGetValue(id, out var item) || item.OrganizationId != organizationId)
                return Task.FromResult(false);

            if (!string.Equals(item.Status, expectedStatus, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(false);

            _items[id] = item with { Status = newStatus, UpdatedAt = DateTime.UtcNow };
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken ct) {
            if (!_items.TryGetValue(id, out var item) || item.OrganizationId != organizationId)
                return Task.FromResult(false);

            return Task.FromResult(_items.Remove(id));
        }
    }

    private sealed class FakeSurveyRepository : ISurveyRepository {
        private readonly Dictionary<(Guid OrgId, Guid SurveyId), dynamic> _surveys = new();

        public void AddSurvey(Guid orgId, Guid surveyId, string title, string status = "active") {
            _surveys[(orgId, surveyId)] = new { id = surveyId, title, status };
        }

        public Task<dynamic?> GetAdminAsync(Guid organizationId, Guid surveyId) {
            _surveys.TryGetValue((organizationId, surveyId), out var s);
            return Task.FromResult<dynamic?>(s);
        }

        public Task<SurveyPublicReadModel?> GetPublicByTokenAsync(string token) => Task.FromResult<SurveyPublicReadModel?>(null);
        public Task<SurveyPublicReadModel?> GetActivePublicSurveyAsync(Guid surveyId) => Task.FromResult<SurveyPublicReadModel?>(null);
        public Task<SurveyPublicReadModel?> GetResultSurveyAsync(Guid surveyId) => Task.FromResult<SurveyPublicReadModel?>(null);
        public Task<bool> LockEligibleForCompletionAsync(SurveyPublicReadModel survey, IDbConnection connection, IDbTransaction transaction) => Task.FromResult(true);
        public Task<SurveyLinkReadModel?> GetPublicLinkAsync(Guid surveyId) => Task.FromResult<SurveyLinkReadModel?>(null);
        public Task<bool> ValidatePublicTokenAsync(Guid surveyId, string token) => Task.FromResult(true);
        public Task<Guid> SaveResponseAsync(Guid surveyId, SubmitResponseRequest request) => Task.FromResult(Guid.NewGuid());
        public Task<int> CountActiveSurveysAsync(Guid organizationId) => Task.FromResult(1);
        public Task<int> CountResponsesThisMonthAsync(Guid organizationId) => Task.FromResult(0);
        public Task<IReadOnlyList<dynamic>> ListAdminAsync(Guid organizationId) => Task.FromResult<IReadOnlyList<dynamic>>([]);
        public Task<Guid> CreateAdminAsync(Guid organizationId, Guid formId, Guid formVersionId, string title, string? description, DateTimeOffset? startsAt, DateTimeOffset? expiresAt) => Task.FromResult(Guid.NewGuid());
        public Task UpdateAdminAsync(Guid organizationId, Guid surveyId, Guid formId, Guid formVersionId, string? title, string? description, DateTimeOffset? startsAt, DateTimeOffset? expiresAt) => Task.CompletedTask;
        public Task UpdateStatusAdminAsync(Guid organizationId, Guid surveyId, string status) => Task.CompletedTask;
        public Task<IReadOnlyList<dynamic>> ListLinksAdminAsync(Guid organizationId, Guid surveyId) => Task.FromResult<IReadOnlyList<dynamic>>([]);
        public Task<Guid> CreateLinkAdminAsync(Guid organizationId, Guid surveyId, string tokenHash, string publicUrl, DateTimeOffset? expiresAt) => Task.FromResult(Guid.NewGuid());
        public Task UpdateLinkStatusAdminAsync(Guid organizationId, Guid linkId, string status) => Task.CompletedTask;
    }
}
