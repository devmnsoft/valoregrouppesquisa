using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ValoraPesquisa.Api.Controllers.V11;
[ApiController,Authorize,Route("api/forms")]
public sealed class FormsV11Controller:ControllerBase{
 [HttpGet] public IActionResult List(CancellationToken ct)=>Ok(new{module="forms",version="v1.1"});
 [HttpGet("{id:guid}")] public IActionResult Get(Guid id,CancellationToken ct)=>Ok(new{id});
 [HttpPost] public IActionResult Create(CancellationToken ct)=>Accepted(new{status="queued"});
 [HttpPost("{id:guid}/sections")] public IActionResult AddSection(Guid id,CancellationToken ct)=>Accepted();
 [HttpPost("{id:guid}/fields")] public IActionResult AddField(Guid id,CancellationToken ct)=>Accepted();
 [HttpPut("fields/{fieldId:guid}")] public IActionResult UpdateField(Guid fieldId,CancellationToken ct)=>Accepted();
 [HttpPost("{id:guid}/rules")] public IActionResult AddRule(Guid id,CancellationToken ct)=>Accepted();
 [HttpPost("{id:guid}/publish")] public IActionResult Publish(Guid id,CancellationToken ct)=>Accepted();
 [HttpGet("{id:guid}/preview")] public IActionResult Preview(Guid id,CancellationToken ct)=>Ok(new{id,preview=true});
 [HttpPost("{id:guid}/responses")] public IActionResult Submit(Guid id,CancellationToken ct)=>Accepted();
 [HttpGet("responses/{responseId:guid}")] public IActionResult Response(Guid responseId,CancellationToken ct)=>Ok(new{responseId}); }
[ApiController,Authorize,Route("api/automation")]
public sealed class AutomationV11Controller:ControllerBase{
 [HttpGet("rules")] public IActionResult Rules(CancellationToken ct)=>Ok(Array.Empty<object>());
 [HttpGet("rules/{id:guid}")] public IActionResult Rule(Guid id,CancellationToken ct)=>Ok(new{id});
 [HttpPost("rules")] public IActionResult Create(CancellationToken ct)=>Accepted();
 [HttpPut("rules/{id:guid}")] public IActionResult Update(Guid id,CancellationToken ct)=>Accepted();
 [HttpPost("rules/{id:guid}/enable")] public IActionResult Enable(Guid id,CancellationToken ct)=>Accepted();
 [HttpPost("rules/{id:guid}/disable")] public IActionResult Disable(Guid id,CancellationToken ct)=>Accepted();
 [HttpPost("rules/{id:guid}/execute")] public IActionResult Execute(Guid id,CancellationToken ct)=>Accepted();
 [HttpGet("executions")] public IActionResult Executions(CancellationToken ct)=>Ok(Array.Empty<object>());
 [HttpPost("executions/{id:guid}/retry")] public IActionResult Retry(Guid id,CancellationToken ct)=>Accepted(); }
[ApiController,Authorize,Route("api/attachments")]
public sealed class AttachmentsV11Controller:ControllerBase{ [HttpGet("entity")] public IActionResult Entity(CancellationToken ct)=>Ok(Array.Empty<object>()); [HttpPost("upload")] public IActionResult Upload(CancellationToken ct)=>Accepted(); [HttpPost("{id:guid}/link")] public IActionResult Link(Guid id,CancellationToken ct)=>Accepted(); [HttpGet("{id:guid}")] public IActionResult Get(Guid id,CancellationToken ct)=>Ok(new{id}); [HttpGet("{id:guid}/download")] public IActionResult Download(Guid id,CancellationToken ct)=>File(Array.Empty<byte>(),"application/octet-stream","placeholder.txt"); [HttpDelete("{id:guid}")] public IActionResult Delete(Guid id,CancellationToken ct)=>NoContent(); }
[ApiController,Authorize,Route("api/notifications")]
public sealed class NotificationsV11Controller:ControllerBase{ [HttpGet] public IActionResult List(CancellationToken ct)=>Ok(Array.Empty<object>()); [HttpPost] public IActionResult Create(CancellationToken ct)=>Accepted(); [HttpPost("{id:guid}/read")] public IActionResult Read(Guid id,CancellationToken ct)=>Accepted(); [HttpGet("preferences")] public IActionResult Prefs(CancellationToken ct)=>Ok(Array.Empty<object>()); [HttpPut("preferences")] public IActionResult UpdatePrefs(CancellationToken ct)=>Accepted(); }
[ApiController,Authorize,Route("api/reports")]
public sealed class ReportsV11Controller:ControllerBase{ [HttpGet] public IActionResult List(CancellationToken ct)=>Ok(Array.Empty<object>()); [HttpGet("{id:guid}")] public IActionResult Get(Guid id,CancellationToken ct)=>Ok(new{id}); [HttpPost] public IActionResult Create(CancellationToken ct)=>Accepted(); [HttpPost("{id:guid}/execute")] public IActionResult Execute(Guid id,CancellationToken ct)=>Accepted(); [HttpPost("{id:guid}/export")] public IActionResult Export(Guid id,CancellationToken ct)=>Accepted(); [HttpGet("executions")] public IActionResult Executions(CancellationToken ct)=>Ok(Array.Empty<object>()); [HttpGet("exports/{exportId:guid}/download")] public IActionResult Download(Guid exportId,CancellationToken ct)=>File(Array.Empty<byte>(),"text/csv","report.csv"); [HttpPost("{id:guid}/schedule")] public IActionResult Schedule(Guid id,CancellationToken ct)=>Accepted(); }
