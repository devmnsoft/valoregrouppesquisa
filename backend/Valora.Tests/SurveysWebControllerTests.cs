extern alias ValoraWeb;

using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.ReadModels;
using WebSurveysController = ValoraWeb::Valora.Web.Controllers.SurveysController;
using SurveysPublicLinksViewModel = ValoraWeb::Valora.Web.Models.ViewModels.SurveysPublicLinksViewModel;
using Xunit;

namespace Valora.Tests;

public sealed class SurveysWebControllerTests {
    private readonly Guid _testOrgId = Guid.NewGuid();
    private readonly Guid _testSurveyId = Guid.NewGuid();

    [Fact]
    public async Task PublicLinks_WhenNoOrganization_ReturnsNoOrganizationView() {
        var context = new TestRequestContext(null);
        var repo = new FakeSurveyRepository();
        var controller = new WebSurveysController(repo, context, NullLogger<WebSurveysController>.Instance);

        var result = await controller.PublicLinks(null);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("NoOrganization", viewResult.ViewName);
    }

    [Fact]
    public async Task PublicLinks_WithOrganization_ReturnsPopulatedViewModel() {
        var context = new TestRequestContext(_testOrgId);
        var repo = new FakeSurveyRepository {
            SurveysToReturn = [
                new { id = _testSurveyId, title = "Clima Organizacional 2026", status = "active", form_name = "Form V1", completed_responses = 12 }
            ],
            LinksToReturn = [
                new {
                    id = Guid.NewGuid(),
                    organization_id = _testOrgId,
                    survey_id = _testSurveyId,
                    public_url = $"/public/surveys/{_testSurveyId}?token=abc",
                    status = "active",
                    starts_at = (DateTimeOffset?)null,
                    expires_at = (DateTimeOffset?)DateTimeOffset.UtcNow.AddDays(10),
                    created_at = DateTimeOffset.UtcNow
                }
            ]
        };

        var controller = new WebSurveysController(repo, context, NullLogger<WebSurveysController>.Instance);
        var result = await controller.PublicLinks(_testSurveyId);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<SurveysPublicLinksViewModel>(viewResult.Model);
        Assert.Equal(1, model.TotalSurveys);
        Assert.Equal(1, model.TotalLinks);
        Assert.Equal(1, model.ActiveLinks);
        Assert.Equal(_testSurveyId, model.SelectedSurveyId);
        Assert.Single(model.Links);
    }

    [Fact]
    public async Task CreateLink_ValidInput_CallsRepositoryAndSetsSuccessMessage() {
        var context = new TestRequestContext(_testOrgId);
        var repo = new FakeSurveyRepository();
        var controller = new WebSurveysController(repo, context, NullLogger<WebSurveysController>.Instance) {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider())
        };

        var result = await controller.CreateLink(_testSurveyId, 15);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WebSurveysController.PublicLinks), redirect.ActionName);
        Assert.NotNull(controller.TempData["SuccessMessage"]);
        Assert.True(repo.CreateLinkCalled);
        Assert.Equal(_testOrgId, repo.LastCreatedOrgId);
        Assert.Equal(_testSurveyId, repo.LastCreatedSurveyId);
    }

    [Fact]
    public async Task UpdateLinkStatus_ValidStatus_CallsRepositoryAndRedirects() {
        var context = new TestRequestContext(_testOrgId);
        var repo = new FakeSurveyRepository();
        var linkId = Guid.NewGuid();
        var controller = new WebSurveysController(repo, context, NullLogger<WebSurveysController>.Instance) {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider())
        };

        var result = await controller.UpdateLinkStatus(linkId, _testSurveyId, "inactive");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WebSurveysController.PublicLinks), redirect.ActionName);
        Assert.NotNull(controller.TempData["SuccessMessage"]);
        Assert.True(repo.UpdateLinkStatusCalled);
        Assert.Equal("inactive", repo.LastUpdatedStatus);
    }

    private sealed class TestRequestContext(Guid? orgId) : ICurrentRequestContext {
        public CurrentRequestContext GetCurrent() =>
            new(Guid.NewGuid(), null, orgId, null, false, [], [], [], [], [], "active", 1);
    }

    private sealed class FakeTempDataProvider : ITempDataProvider {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class FakeSurveyRepository : ISurveyRepository {
        public List<dynamic> SurveysToReturn { get; set; } = [];
        public List<dynamic> LinksToReturn { get; set; } = [];
        public bool CreateLinkCalled { get; private set; }
        public Guid LastCreatedOrgId { get; private set; }
        public Guid LastCreatedSurveyId { get; private set; }
        public bool UpdateLinkStatusCalled { get; private set; }
        public string? LastUpdatedStatus { get; private set; }

        public Task<IReadOnlyList<dynamic>> ListAdminAsync(Guid organizationId) =>
            Task.FromResult<IReadOnlyList<dynamic>>(SurveysToReturn);

        public Task<IReadOnlyList<dynamic>> ListLinksAdminAsync(Guid organizationId, Guid surveyId) =>
            Task.FromResult<IReadOnlyList<dynamic>>(LinksToReturn);

        public Task<Guid> CreateLinkAdminAsync(Guid organizationId, Guid surveyId, string tokenHash, string publicUrl, DateTimeOffset? expiresAt) {
            CreateLinkCalled = true;
            LastCreatedOrgId = organizationId;
            LastCreatedSurveyId = surveyId;
            return Task.FromResult(Guid.NewGuid());
        }

        public Task UpdateLinkStatusAdminAsync(Guid organizationId, Guid linkId, string status) {
            UpdateLinkStatusCalled = true;
            LastUpdatedStatus = status;
            return Task.CompletedTask;
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
        public Task<dynamic?> GetAdminAsync(Guid organizationId, Guid surveyId) => Task.FromResult<dynamic?>(null);
        public Task<Guid> CreateAdminAsync(Guid organizationId, Guid formId, Guid formVersionId, string title, string? description, DateTimeOffset? startsAt, DateTimeOffset? expiresAt) => Task.FromResult(Guid.NewGuid());
        public Task UpdateAdminAsync(Guid organizationId, Guid surveyId, Guid formId, Guid formVersionId, string? title, string? description, DateTimeOffset? startsAt, DateTimeOffset? expiresAt) => Task.CompletedTask;
        public Task UpdateStatusAdminAsync(Guid organizationId, Guid surveyId, string status) => Task.CompletedTask;
    }
}
