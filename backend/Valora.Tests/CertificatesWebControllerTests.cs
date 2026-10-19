extern alias ValoraWeb;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using WebCertificatesController = ValoraWeb::Valora.Web.Controllers.CertificatesController;
using CertificatesIndexViewModel = ValoraWeb::Valora.Web.Models.ViewModels.CertificatesIndexViewModel;
using Xunit;

namespace Valora.Tests;

public sealed class CertificatesWebControllerTests {
    private readonly Guid _testOrgId = Guid.NewGuid();
    private readonly Guid _testCertId = Guid.NewGuid();

    [Fact]
    public async Task Index_WhenNoOrganization_ReturnsNoOrganizationView() {
        var context = new TestRequestContext(null);
        var service = new FakeCertificateOperationalService();
        var controller = new WebCertificatesController(service, context, NullLogger<WebCertificatesController>.Instance);

        var result = await controller.Index(null);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("NoOrganization", viewResult.ViewName);
    }

    [Fact]
    public async Task Index_WithOrganization_ReturnsPopulatedViewModel() {
        var context = new TestRequestContext(_testOrgId);
        var service = new FakeCertificateOperationalService {
            CertsToReturn = [
                new CertificateDto(
                    _testCertId,
                    _testOrgId,
                    null,
                    Guid.NewGuid(),
                    "Mariana Silva",
                    "m***@empresa.com",
                    "Acme Corp",
                    85.5m,
                    "Nível Avançado",
                    "VALORA-CERT-12345",
                    "/public/certificates/validate?code=VALORA-CERT-12345",
                    "issued",
                    DateTimeOffset.UtcNow,
                    "{}"
                )
            ]
        };

        var controller = new WebCertificatesController(service, context, NullLogger<WebCertificatesController>.Instance);
        var result = await controller.Index(null);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CertificatesIndexViewModel>(viewResult.Model);
        Assert.Equal(1, model.TotalCertificates);
        Assert.Equal(1, model.IssuedCount);
        Assert.Equal(0, model.RevokedCount);
        Assert.Single(model.Certificates);
        Assert.Equal("VALORA-CERT-12345", model.Certificates[0].ValidationCode);
    }

    [Fact]
    public async Task Revoke_ValidInput_CallsRevokeAsyncAndRedirects() {
        var context = new TestRequestContext(_testOrgId);
        var service = new FakeCertificateOperationalService();
        var controller = new WebCertificatesController(service, context, NullLogger<WebCertificatesController>.Instance) {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider())
        };

        var result = await controller.Revoke(_testCertId);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WebCertificatesController.Index), redirect.ActionName);
        Assert.NotNull(controller.TempData["SuccessMessage"]);
        Assert.True(service.RevokeCalled);
        Assert.Equal(_testCertId, service.LastRevokedCertId);
    }

    [Fact]
    public async Task Download_ValidInput_ReturnsFileResult() {
        var context = new TestRequestContext(_testOrgId);
        var service = new FakeCertificateOperationalService();
        var controller = new WebCertificatesController(service, context, NullLogger<WebCertificatesController>.Instance);

        var result = await controller.Download(_testCertId);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/html; charset=utf-8", fileResult.ContentType);
        Assert.Contains("Certificado-Valora", fileResult.FileDownloadName);
    }

    private sealed class TestRequestContext(Guid? orgId) : ICurrentRequestContext {
        public CurrentRequestContext GetCurrent() =>
            new(Guid.NewGuid(), null, orgId, null, false, [], [], [], [], [], "active", 1);
    }

    private sealed class FakeTempDataProvider : ITempDataProvider {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class FakeCertificateOperationalService : ICertificateOperationalService {
        public List<CertificateDto> CertsToReturn { get; set; } = [];
        public bool RevokeCalled { get; private set; }
        public Guid LastRevokedCertId { get; private set; }

        public Task<CertificateDto> GenerateAsync(Guid organizationId, Guid responseId) =>
            Task.FromResult(new CertificateDto(Guid.NewGuid(), organizationId, null, responseId, "Teste", "t***@empresa.com", "Empresa", 80m, "Avançado", "VALORA-123", null, "issued", DateTimeOffset.UtcNow, "{}"));

        public Task<IReadOnlyList<CertificateDto>> ListAsync(Guid organizationId) =>
            Task.FromResult<IReadOnlyList<CertificateDto>>(CertsToReturn);

        public Task<CertificateDto?> GetAsync(Guid organizationId, Guid id) =>
            Task.FromResult<CertificateDto?>(CertsToReturn.Find(c => c.Id == id) ?? new CertificateDto(id, organizationId, null, Guid.NewGuid(), "Teste", "t***@empresa.com", "Empresa", 80m, "Avançado", "VALORA-123", null, "issued", DateTimeOffset.UtcNow, "{}"));

        public Task<string?> DownloadHtmlAsync(Guid organizationId, Guid id) =>
            Task.FromResult<string?>("<html><body><h1>Certificado Valora</h1></body></html>");

        public Task RevokeAsync(Guid organizationId, Guid id) {
            RevokeCalled = true;
            LastRevokedCertId = id;
            return Task.CompletedTask;
        }
    }
}
