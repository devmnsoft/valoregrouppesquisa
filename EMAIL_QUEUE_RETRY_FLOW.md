# Fluxo de Fila e Retry de E-mail
Coleção `emailJobs` com campos mínimos: id, type, status, companyId, surveyId, responseId, participantEmailMasked, toHash, subject, template, attempts, maxAttempts, lastAttemptAt, nextAttemptAt, sentAt, failedAt, failedReason, createdAt, updatedAt, createdBy, source.

Status permitidos: pending, sending, sent, failed, pending_retry, dead_letter, cancelled.

Regras: ao finalizar pesquisa ou reenvio manual, criar emailJob com responseId real; nunca criar job resp_demo; nunca criar sem responseId real; usar e-mail mascarado e hash; attempts tem limite por maxAttempts; falhas transitórias viram pending_retry; falhas definitivas viram failed/dead_letter; retry manual só admin_valora; retry não duplica envio sem confirmação; todos envios geram auditLog.

Functions: processEmailJob, retryEmailJob, resendResultEmail, getEmailJobStatus, scheduledProcessEmailRetries.
