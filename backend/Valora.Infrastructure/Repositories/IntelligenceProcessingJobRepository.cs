using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.OrganizationalIntelligence;

namespace Valora.Infrastructure.Repositories;

public sealed class IntelligenceProcessingJobRepository(IDbConnectionFactory connections) : IIntelligenceProcessingJobRepository
{
    private const string JobProjection = """
        id AS "Id", organization_id AS "OrganizationId", survey_id AS "SurveyId",
        response_id AS "ResponseId", form_id AS "FormId", source_entity_id AS "SourceEntityId",
        trigger AS "Trigger", status AS "Status", priority AS "Priority", attempts AS "Attempts",
        max_attempts AS "MaxAttempts", scheduled_at AS "ScheduledAt", started_at AS "StartedAt",
        completed_at AS "CompletedAt", failed_at AS "FailedAt", next_attempt_at AS "NextAttemptAt",
        locked_at AS "LockedAt", locked_by AS "LockedBy", run_id AS "RunId",
        error_code AS "ErrorCode", error_message AS "ErrorMessage", correlation_id AS "CorrelationId",
        idempotency_key AS "IdempotencyKey", metadata_json::text AS "MetadataJson",
        created_at AS "CreatedAt", updated_at AS "UpdatedAt", deleted_at AS "DeletedAt"
        """;

    private const string StageProjection = """
        id AS "Id", organization_id AS "OrganizationId", job_id AS "JobId", run_id AS "RunId",
        stage AS "Stage", status AS "Status", records AS "Records",
        sufficient_evidence AS "SufficientEvidence", message AS "Message",
        started_at AS "StartedAt", completed_at AS "CompletedAt", duration_ms AS "DurationMs",
        error_code AS "ErrorCode", error_message AS "ErrorMessage",
        evidence_ids::text AS "EvidenceJson", metadata_json::text AS "MetadataJson",
        created_at AS "CreatedAt"
        """;
    public async Task<Guid> EnqueueAsync(IntelligenceProcessingContext c, int maxAttempts, string correlationId, CancellationToken ct)
    {
        const string sql = """INSERT INTO valorapesquisa.intelligence_processing_jobs(organization_id,survey_id,response_id,form_id,source_entity_id,trigger,status,max_attempts,correlation_id,idempotency_key,metadata_json) VALUES(@OrganizationId,@SurveyId,@ResponseId,@FormId,@SourceEntityId,@Trigger,'pending',@maxAttempts,@correlationId,md5(@OrganizationId::text||':'||coalesce(@ResponseId::text,@SurveyId::text,@SourceEntityId::text,'all')||':'||@Trigger),jsonb_build_object('source','valora_pipeline')) ON CONFLICT (organization_id,idempotency_key) WHERE deleted_at IS NULL AND status IN ('pending','running','retry_scheduled') DO UPDATE SET updated_at=now() RETURNING id""";
        using var db=connections.Create(); return await db.ExecuteScalarAsync<Guid>(new CommandDefinition(sql,new { c.OrganizationId,c.SurveyId,c.ResponseId,c.FormId,c.SourceEntityId,c.Trigger,maxAttempts,correlationId},cancellationToken:ct));
    }
    public async Task<IReadOnlyList<IntelligenceProcessingJob>> GetPendingJobsAsync(int take, CancellationToken ct)
    { var sql=$"SELECT {JobProjection} FROM valorapesquisa.intelligence_processing_jobs WHERE deleted_at IS NULL AND status IN ('pending','retry_scheduled') AND scheduled_at<=now() AND coalesce(next_attempt_at,now())<=now() AND (locked_at IS NULL OR locked_at<now()-interval '10 minutes') ORDER BY priority,scheduled_at LIMIT @take"; using var db=connections.Create(); var rows=await db.QueryAsync<IntelligenceProcessingJobRow>(new CommandDefinition(sql,new{take},cancellationToken:ct)); return rows.Select(ToContract).ToList(); }
    public async Task<bool> LockJobAsync(Guid id,string worker,CancellationToken ct)
    { const string sql="""UPDATE valorapesquisa.intelligence_processing_jobs SET locked_at=now(),locked_by=@worker,updated_at=now() WHERE id=@id AND status IN ('pending','retry_scheduled') AND (locked_at IS NULL OR locked_at<now()-interval '10 minutes')"""; using var db=connections.Create(); return await db.ExecuteAsync(new CommandDefinition(sql,new{id,worker},cancellationToken:ct))==1; }
    public Task MarkRunningAsync(Guid id,Guid runId,CancellationToken ct)=>Execute("UPDATE valorapesquisa.intelligence_processing_jobs SET status='running',started_at=coalesce(started_at,now()),attempts=attempts+1,run_id=@runId,updated_at=now() WHERE id=@id",new{id,runId},ct);
    public Task MarkCompletedAsync(Guid id,string status,CancellationToken ct)=>Execute("UPDATE valorapesquisa.intelligence_processing_jobs SET status=@status,completed_at=now(),locked_at=NULL,locked_by=NULL,error_code=NULL,error_message=NULL,updated_at=now() WHERE id=@id",new{id,status},ct);
    public Task MarkFailedAsync(Guid id,string code,string message,CancellationToken ct)=>Execute("UPDATE valorapesquisa.intelligence_processing_jobs SET status='failed',failed_at=now(),locked_at=NULL,locked_by=NULL,error_code=@code,error_message=@message,updated_at=now() WHERE id=@id",new{id,code,message},ct);
    public Task ScheduleRetryAsync(Guid id,DateTime next,string code,string message,CancellationToken ct)=>Execute("UPDATE valorapesquisa.intelligence_processing_jobs SET status='retry_scheduled',next_attempt_at=@next,locked_at=NULL,locked_by=NULL,error_code=@code,error_message=@message,updated_at=now() WHERE id=@id",new{id,next,code,message},ct);
    public async Task<bool> CancelAsync(Guid o,Guid id,Guid? user,CancellationToken ct)
    { const string sql="""WITH changed AS (UPDATE valorapesquisa.intelligence_processing_jobs SET status='cancelled',locked_at=NULL,locked_by=NULL,updated_at=now() WHERE id=@id AND organization_id=@o AND status IN ('pending','retry_scheduled') RETURNING id) INSERT INTO valorapesquisa.intelligence_reprocess_requests(organization_id,job_id,requested_by,request_type,status,metadata_json) SELECT @o,id,@user,'cancel','completed','{}'::jsonb FROM changed RETURNING true"""; using var db=connections.Create(); return await db.ExecuteScalarAsync<bool?>(new CommandDefinition(sql,new{o,id,user},cancellationToken:ct))??false; }
    public async Task<Guid?> ReprocessAsync(Guid o,Guid id,Guid? user,string correlationId,CancellationToken ct)
    { const string sql="""WITH source AS (SELECT id,organization_id,survey_id,response_id,form_id,source_entity_id,trigger,max_attempts FROM valorapesquisa.intelligence_processing_jobs WHERE id=@id AND organization_id=@o AND deleted_at IS NULL), created AS (INSERT INTO valorapesquisa.intelligence_processing_jobs(organization_id,survey_id,response_id,form_id,source_entity_id,trigger,status,max_attempts,correlation_id,metadata_json,idempotency_key) SELECT organization_id,survey_id,response_id,form_id,source_entity_id,'reprocess:'||trigger,'pending',max_attempts,@correlationId,jsonb_build_object('reprocessedJobId',id),md5(id::text||':'||clock_timestamp()::text) FROM source RETURNING id) INSERT INTO valorapesquisa.intelligence_reprocess_requests(organization_id,job_id,requested_by,request_type,status,correlation_id,metadata_json) SELECT @o,id,@user,'job','pending',@correlationId,'{}'::jsonb FROM created RETURNING job_id"""; using var db=connections.Create(); return await db.ExecuteScalarAsync<Guid?>(new CommandDefinition(sql,new{o,id,user,correlationId},cancellationToken:ct)); }
    public async Task<IReadOnlyList<IntelligenceProcessingJob>> ListJobsAsync(Guid o,IntelligenceJobFilter f,CancellationToken ct)
    { var sql=$"SELECT {JobProjection} FROM valorapesquisa.intelligence_processing_jobs WHERE organization_id=@o AND deleted_at IS NULL AND (@Status IS NULL OR status=@Status) AND (@SurveyId IS NULL OR survey_id=@SurveyId) AND (@ResponseId IS NULL OR response_id=@ResponseId) AND (@Trigger IS NULL OR trigger=@Trigger) ORDER BY created_at DESC OFFSET @offset LIMIT @PageSize"; using var db=connections.Create(); var rows=await db.QueryAsync<IntelligenceProcessingJobRow>(new CommandDefinition(sql,new{o,f.Status,f.SurveyId,f.ResponseId,f.Trigger,offset=(Math.Max(1,f.Page)-1)*Math.Clamp(f.PageSize,1,100),PageSize=Math.Clamp(f.PageSize,1,100)},cancellationToken:ct)); return rows.Select(ToContract).ToList(); }
    public async Task<IntelligenceProcessingJob?> GetJobAsync(Guid o,Guid id,CancellationToken ct) { using var db=connections.Create(); var sql=$"SELECT {JobProjection} FROM valorapesquisa.intelligence_processing_jobs WHERE organization_id=@o AND id=@id AND deleted_at IS NULL"; var row=await db.QuerySingleOrDefaultAsync<IntelligenceProcessingJobRow>(new CommandDefinition(sql,new{o,id},cancellationToken:ct)); return row is null?null:ToContract(row); }
    public async Task<IReadOnlyList<IntelligenceStageRun>> ListStageRunsAsync(Guid o,Guid id,CancellationToken ct) { using var db=connections.Create(); var sql=$"SELECT {StageProjection} FROM valorapesquisa.intelligence_pipeline_stage_runs WHERE organization_id=@o AND job_id=@id ORDER BY started_at"; var rows=await db.QueryAsync<StageRow>(new CommandDefinition(sql,new{o,id},cancellationToken:ct)); return rows.Select(x=>new IntelligenceStageRun(x.Id,x.JobId,x.RunId,x.Stage,x.Status,x.Records,x.SufficientEvidence,x.Message,x.StartedAt,x.CompletedAt,x.DurationMs,x.ErrorCode,x.ErrorMessage,DeserializeEvidence(x.EvidenceJson))).ToList(); }
    public async Task<IntelligenceProcessingSummary> GetSummaryAsync(Guid o,CancellationToken ct) { const string sql="""SELECT count(*) FILTER(WHERE status='pending')::int Pending,count(*) FILTER(WHERE status='running')::int Running,count(*) FILTER(WHERE status='completed' AND completed_at::date=current_date)::int CompletedToday,count(*) FILTER(WHERE status='failed')::int Failed,count(*) FILTER(WHERE status='retry_scheduled')::int RetryScheduled,count(*) FILTER(WHERE status='insufficient_evidence')::int InsufficientEvidence,coalesce(avg(extract(epoch FROM(completed_at-started_at))*1000) FILTER(WHERE completed_at IS NOT NULL),0) AverageDurationMs,(SELECT stage FROM valorapesquisa.intelligence_pipeline_stage_runs s WHERE s.organization_id=@o AND s.status='failed' GROUP BY stage ORDER BY count(*) DESC LIMIT 1) MostFailedStage,max(completed_at) FILTER(WHERE trigger LIKE '%response%') LastResponseProcessedAt,max(completed_at) FILTER(WHERE trigger LIKE '%diagnosis%') LastDiagnosisProcessedAt FROM valorapesquisa.intelligence_processing_jobs WHERE organization_id=@o AND deleted_at IS NULL"""; using var db=connections.Create(); return await db.QuerySingleAsync<IntelligenceProcessingSummary>(new CommandDefinition(sql,new{o},cancellationToken:ct)); }
    public Task LogStageAsync(Guid o,Guid job,Guid run,ProcessingStageResult s,string status,DateTime from,DateTime to,CancellationToken ct)=>Execute("INSERT INTO valorapesquisa.intelligence_pipeline_stage_runs(organization_id,job_id,run_id,stage,status,records,sufficient_evidence,message,started_at,completed_at,duration_ms,evidence_ids,metadata_json) VALUES(@o,@job,@run,@Stage,@status,@Records,@SufficientEvidence,@Message,@from,@to,@duration,CAST(@evidence AS jsonb),'{}'::jsonb)",new{o,job,run,s.Stage,s.Records,s.SufficientEvidence,s.Message,from,to,duration=(long)(to-from).TotalMilliseconds,evidence=JsonSerializer.Serialize(s.EvidenceIds)},ct);
    private async Task Execute(string sql,object args,CancellationToken ct) { using var db=connections.Create(); await db.ExecuteAsync(new CommandDefinition(sql,args,cancellationToken:ct)); }
    private static IntelligenceProcessingJob ToContract(IntelligenceProcessingJobRow x) => new(x.Id,x.OrganizationId,x.SurveyId,x.ResponseId,x.FormId,x.SourceEntityId,x.Trigger,x.Status,x.Priority,x.Attempts,x.MaxAttempts,x.ScheduledAt,x.StartedAt,x.CompletedAt,x.FailedAt,x.NextAttemptAt,x.LockedBy,x.ErrorCode,x.ErrorMessage,x.CorrelationId,x.CreatedAt);
    private static IReadOnlyList<Guid> DeserializeEvidence(string? json) => string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<Guid>>(json) ?? [];

    private sealed class IntelligenceProcessingJobRow
    {
        public Guid Id { get; set; } public Guid OrganizationId { get; set; } public Guid? SurveyId { get; set; }
        public Guid? ResponseId { get; set; } public Guid? FormId { get; set; } public Guid? SourceEntityId { get; set; }
        public string Trigger { get; set; } = string.Empty; public string Status { get; set; } = string.Empty;
        public int Priority { get; set; } public int Attempts { get; set; } public int MaxAttempts { get; set; }
        public DateTime ScheduledAt { get; set; } public DateTime? StartedAt { get; set; } public DateTime? CompletedAt { get; set; }
        public DateTime? FailedAt { get; set; } public DateTime? NextAttemptAt { get; set; } public DateTime? LockedAt { get; set; }
        public string? LockedBy { get; set; } public Guid? RunId { get; set; } public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; } public string? CorrelationId { get; set; } public string? IdempotencyKey { get; set; }
        public string? MetadataJson { get; set; } public DateTime CreatedAt { get; set; } public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
    private sealed class StageRow
    {
        public Guid Id { get; set; } public Guid OrganizationId { get; set; } public Guid JobId { get; set; }
        public Guid RunId { get; set; } public string Stage { get; set; } = string.Empty; public string Status { get; set; } = string.Empty;
        public int Records { get; set; } public bool SufficientEvidence { get; set; } public string Message { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; } public DateTime? CompletedAt { get; set; } public long? DurationMs { get; set; }
        public string? ErrorCode { get; set; } public string? ErrorMessage { get; set; } public string? EvidenceJson { get; set; }
        public string? MetadataJson { get; set; } public DateTime CreatedAt { get; set; }
    }
}
