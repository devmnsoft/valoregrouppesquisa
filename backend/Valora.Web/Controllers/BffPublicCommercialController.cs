using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[ApiController, AllowAnonymous, Route("bff/public")]
public sealed class BffPublicCommercialController(IBffApiClient api) : ControllerBase
{
    [HttpPost("diagnostic/start")]
    public Task Start(CancellationToken ct)=>Proxy(HttpMethod.Post,"/api/v1/public/diagnostic/start",ct);
    [HttpPost("contact-requests")]
    public Task Contact(CancellationToken ct)=>Proxy(HttpMethod.Post,"/api/v1/public/contact-requests",ct);
    [HttpPost("plan-interest")]
    public Task Plan(CancellationToken ct)=>Proxy(HttpMethod.Post,"/api/v1/public/plan-interest",ct);
    [AcceptVerbs("GET", "POST")]
    [Route("surveys/{**resource}")]
    public Task Surveys(string? resource,CancellationToken ct)=>Proxy(new HttpMethod(Request.Method),$"/public/surveys/{resource}{Request.QueryString}",ct);
    [AcceptVerbs("GET", "POST")]
    [Route("results/{**resource}")]
    public Task Results(string? resource,CancellationToken ct)=>Proxy(new HttpMethod(Request.Method),$"/public/results/{resource}{Request.QueryString}",ct);
    [HttpGet("plans")]
    public Task Plans(CancellationToken ct)=>Proxy(HttpMethod.Get,"/plans/public",ct);
    [HttpGet("certificates/validate/{code}")]
    public Task Certificate(string code,CancellationToken ct)=>Proxy(HttpMethod.Get,$"/certificates/validate/{Uri.EscapeDataString(code)}",ct);
    private async Task Proxy(HttpMethod method,string path,CancellationToken ct)
    {
        object? body=null;
        if(Request.ContentLength>0) body=await System.Text.Json.JsonSerializer.DeserializeAsync<object>(Request.Body,cancellationToken:ct);
        using var response=await api.SendAsync(method,path,body,string.Empty,HttpContext.TraceIdentifier,ct);
        Response.StatusCode=(int)response.StatusCode;
        Response.ContentType=response.Content.Headers.ContentType?.ToString()??"application/json";
        await response.Content.CopyToAsync(Response.Body,ct);
    }
}
