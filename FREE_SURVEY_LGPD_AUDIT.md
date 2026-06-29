# Auditoria LGPD da pesquisa gratuita

Eventos obrigatórios: `free_survey.started`, `free_survey.submitted`, `free_survey.result_generated`, `free_survey.email_job_created`, `free_survey.email_sent`, `free_survey.email_failed`, `free_survey.certificate_generated`, `free_survey.whatsapp_cta_clicked`, `free_survey.result_email_resent`, `free_survey.certificate_regenerated`.

Cada evento deve registrar `organizationId` quando houver, `surveyId`, `responseId`, `eventType`, `actorType` (`public`, `user` ou `system`), `ipHash` ou `userAgentHash` quando aplicável, `createdAt` e metadata sanitizada.

É proibido registrar corpo completo do e-mail, token puro, resultToken puro, senha, SMTP password, stack trace ou dados sensíveis desnecessários. O painel operacional só exibe dados mascarados e erro sanitizado.
