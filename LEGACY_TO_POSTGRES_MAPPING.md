# Mapeamento Legado → PostgreSQL Oficial

Todos os relatórios devem mascarar senha, hash, token, refresh token, connection string, SMTP secret e payload sensível.

| Domínio | Fontes legadas | Destino oficial | Transformação | Deduplicação | Conflito | Obrigatório | Sensível/máscara |
|---|---|---|---|---|---|---|---|
| Organizações | `companies`, `organizations`, `companyProfiles` | `organizations`, `organization_settings`, `organization_branding`, `subscriptions`, `organization_modules` | nome trim, slug seguro, documento só dígitos, status oficial | documento → slug → e-mail → nome normalizado | documento/slug divergente é bloqueante | nome/status | documento/e-mail mascarados |
| Usuários | `users`, `authUsers`, `companyUsers`, `participants` | `users`, `user_profiles`, `roles`, `permissions`, `role_permissions`, `survey_participants` | e-mail minúsculo, perfil legado para oficial, sem senha em texto | e-mail → legacyId → documento | e-mail com organização/perfil incompatível | e-mail/perfil | e-mail/documento mascarados; senha não importa |
| Planos e módulos | `plans`, `modules`, `companyModules`, `subscription` | `plans`, `plan_limits`, `plan_capabilities`, `modules`, `organization_modules`, `subscriptions`, `usage_monthly` | códigos lower/slug, status normalizado | código/legacyId | limite incompatível é warning ou bloqueante se impede acesso | plano/código | sem segredo |
| Formulários | `forms`, `questions`, `dimensions`, `options` | `forms`, `form_dimensions`, `questions`, `question_options` | tipos de pergunta/resposta para enum oficial | legacyId → organização+título+versão | estrutura de questões divergente é bloqueante | título/perguntas | textos revisáveis |
| Pesquisas | `surveys`, `publicLinks`, `surveyLinks`, `invites` | `surveys`, `survey_links`, `survey_invites`, `survey_participants` | datas UTC, token público regenerado/hash | legacyId → organização+título+data | link público bruto não é importado | survey/form/org | token bruto mascarado/regenerado |
| Respostas | `responses`, `answers`, `results`, `scores` | `responses`, `response_answers`, `result_scores`, `dimension_scores` | scores numéricos, completedAt UTC | legacyId → survey+participantEmail+completedAt → hash answers | resposta sem survey/form é bloqueante | survey/answers | e-mail mascarado |
| Certificados | `certificates` | `certificates`, `certificate_validations` | código de validação vira hash ou novo código | validationCode → responseId → legacyId | certificado sem response é bloqueante | response | código mascarado/hash |
| Comunicação/e-mail | `emailJobs`, `communications`, `outbox` | `communications`, `email_templates`, `email_jobs` | status oficial, body revisável | legacyId + entidade | SMTP/segredo bloqueado | destinatário/status | e-mail mascarado; SMTP não importa |
| Auditoria | `auditLogs`, `logs`, `events` | `audit_logs` | payload sanitizado, sem stack trace | timestamp+actor+action | payload sensível bloqueado | ação/data | payload mascarado |
| LGPD | `consents`, `privacyRequests` | `lgpd_consents`, `privacy_requests` | e-mail normalizado, datas UTC | e-mail+tipo+data | solicitação sem organização é bloqueante | e-mail/tipo | e-mail/documento mascarados |

Campos sem destino claro seguem para `migration_conflicts` com `manual_review` e não são aplicados automaticamente.
