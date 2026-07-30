using Xunit;
using Microsoft.AspNetCore.Mvc;
using Valora.Api.Controllers;
using Valora.Application.DTOs;

namespace Valora.Tests;

[Trait("Category", "Unit")]
public sealed class PrivacyRequestProtocolContractTests
{
    [Fact]
    public void Privacy_requests_use_public_protocol_not_raw_identifier()
    {
        var protocolProperty = typeof(PrivacyRequestDto).GetProperty(nameof(PrivacyRequestDto.Protocol));
        Assert.NotNull(protocolProperty);
        Assert.Equal(typeof(string), protocolProperty.PropertyType);

        var endpoint = typeof(LgpdController).GetMethod(nameof(LgpdController.PublicGet));
        Assert.NotNull(endpoint);
        var route = Assert.Single(endpoint.GetCustomAttributes(typeof(HttpGetAttribute), false).Cast<HttpGetAttribute>());
        Assert.Equal("/public/lgpd/requests/{protocol}", route.Template);
        Assert.Equal(typeof(string), Assert.Single(endpoint.GetParameters()).ParameterType);
    }
}
