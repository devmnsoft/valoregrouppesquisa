namespace ValoraPesquisa.Domain.Automation;
public enum AutomationTriggerType { RecordCreated, RecordUpdated, RecordStatusChanged, TaskCreated, TaskCompleted, TaskOverdue, ProcessStarted, ProcessCompleted, OrderConfirmed, InvoiceIssued, TitleOverdue, DeliveryOccurrenceCreated, PodRegistered, KpiCritical, ScheduleDaily, ScheduleWeekly, Manual }
public enum AutomationConditionType { Equals, NotEquals, Contains, GreaterThan, LessThan, IsEmpty, IsNotEmpty }
public enum AutomationActionType { CreateTask, StartWorkflow, UpdateRecord, UpdateStatus, SendNotification, QueueOutbox, CalculateKpi, CreateProjectCard, CallWebhookFake, OpenAiHumanEscalation, WriteAuditEvent }
public enum AutomationExecutionStatus { Pending, Running, Completed, Failed, Retrying, Skipped }
public sealed record AutomationRule(Guid Id, Guid TenantId, string Name, bool Enabled, AutomationTriggerType TriggerType);
public sealed record AutomationTrigger(Guid Id, Guid RuleId, AutomationTriggerType Type, string? ConfigurationJson);
public sealed record AutomationCondition(Guid Id, Guid RuleId, AutomationConditionType Type, string Field, string? ExpectedValue);
public sealed record AutomationAction(Guid Id, Guid RuleId, AutomationActionType Type, string ConfigurationJson, int DisplayOrder);
public sealed record AutomationExecution(Guid Id, Guid RuleId, Guid TenantId, AutomationExecutionStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? FinishedAt, int Attempts);
public sealed record AutomationExecutionLog(Guid Id, Guid ExecutionId, string Level, string Message, DateTimeOffset CreatedAt);
