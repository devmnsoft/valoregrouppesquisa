extern alias ValoraWeb;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using EvolutionNumberModelBinder = ValoraWeb::Valora.Web.Models.Binding.EvolutionNumberModelBinder;

namespace Valora.Tests;

public sealed class EvolutionNumberBindingTests {
    [Fact]
    public async Task HtmlNumberIsNotInterpretedAsThousandsInBrazilianCulture() {
        var form = new FormCollection(new Dictionary<string, StringValues> { ["TargetScore"] = "85.5" });
        var provider = new FormValueProvider(BindingSource.Form, form, CultureInfo.GetCultureInfo("pt-BR"));
        var context = DefaultModelBindingContext.CreateBindingContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            provider, new EmptyModelMetadataProvider().GetMetadataForType(typeof(decimal?)), null, "TargetScore");
        await new EvolutionNumberModelBinder(NullLoggerFactory.Instance).BindModelAsync(context);
        Assert.True(context.Result.IsModelSet);
        Assert.Equal(85.5m, Assert.IsType<decimal>(context.Result.Model));
        Assert.Same(provider, context.ValueProvider);
    }
}
