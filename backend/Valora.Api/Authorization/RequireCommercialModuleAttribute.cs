using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Valora.Application.Common;
using Valora.Application.ModularSaas;

namespace Valora.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireCommercialModuleAttribute(string module) : Attribute, IAsyncActionFilter {
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        var http = context.HttpContext;
        var current = http.RequestServices.GetRequiredService<ICurrentRequestContext>().GetCurrent();
        if (current.EffectiveOrganizationId is not { } organization || organization == Guid.Empty) {
            context.Result = new ObjectResult(new { code = "ORGANIZATION_SCOPE_REQUIRED", message = "Selecione uma organização para operar esta área.", correlationId = http.TraceIdentifier }) { StatusCode = 403 };
            return;
        }
        var write = !HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method);
        var commercial = http.RequestServices.GetRequiredService<ICommercialSaasRepository>();
        var decision = await commercial.EvaluateAccessAsync(organization, module, write, http.RequestAborted);
        if (!decision.Allowed || (write && decision.ReadOnly)) {
            context.Result = new ObjectResult(new { code = decision.Code, message = decision.Message, correlationId = http.TraceIdentifier }) { StatusCode = 403 };
            return;
        }
        if (decision.ReadOnly) http.Response.Headers["X-Valora-Module-Access"] = "read-only";
        await next();
    }
}
