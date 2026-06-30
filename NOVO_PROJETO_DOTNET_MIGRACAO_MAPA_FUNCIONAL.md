# Novo Projeto .NET — Mapa Funcional da Migração

## Decisão inicial
A migração continuará em `backend/Valora.sln`, pois a solução já está separada em `Valora.Domain`, `Valora.Application`, `Valora.Infrastructure`, `Valora.Api`, `Valora.Web` e `Valora.Tests`. A estrutura existente já usa ASP.NET Core, MVC/Razor, Web API, Dapper e PostgreSQL, portanto criar `backend-v2` agora duplicaria esforço e aumentaria risco de divergência. O legado JavaScript/Firebase permanece preservado na raiz como referência funcional até o cutover.

## Fontes legadas lidas
Foram usados como referência funcional: `README.md`, `role-definitions.js`, `module-definitions.js`, `repository.js`, `functions/index.js`, `PERFIS_E_PERMISSOES.md`, `MODULOS_E_PLANOS.md`, `ASPNET_WEB_API_GAPS.md` e documentos de auditoria já existentes no repositório.

## 1. Módulos existentes no legado
- `clientes`: cadastro comercial e operacional de empresas.
- `financeiro`: receita, faturas e informações comerciais.
- `planos`: catálogo, limites e módulos liberados.
- `modulos`: matriz global de módulos.
- `usuarios`: usuários e perfis administrativos.
- `funcionarios`: funcionários, respondentes, gestores e convidados.
- `formularios`: questionários, provas, dimensões e pontuação.
- `pesquisas`: campanhas, status, validade e links seguros.
- `convitesEmail`: convites e lembretes por e-mail.
- `respostas`: respostas individuais e consolidadas.
- `relatorios`: indicadores, gráficos e relatórios.
- `certificados`: certificados individuais e validação pública.
- `actionPlans`: planos de ação.
- `valorabot`: assistente contextual.
- `support`: tickets, SLA e atendimento.
- `lgpd`: consentimento e direitos do titular.
- `integrations`: API, webhooks e importações/exportações.
- `exportacoes`: exportação de dados e relatórios.
- `benchmark`: comparativos internos e evolutivos.
- `whiteLabel`: identidade visual e slug público por empresa.
- `backup`: backup/importação/exportação.
- `logs`: auditoria e eventos sensíveis.

## 2. Telas existentes no legado
- Home institucional, diagnóstico gratuito, planos e privacidade/LGPD.
- Login, recuperação de senha, perfil e configurações.
- Portal Valora: dashboard, clientes, planos, módulos, usuários globais, financeiro, logs, auditoria, suporte, configurações e migração.
- Portal empresa: dashboard, dados da empresa, usuários, funcionários, formulários, pesquisas, links, convites, respostas, relatórios, certificados, comunicações, configurações e LGPD.
- Portal participante: pesquisas disponíveis, resposta pública, resultados, certificados e solicitações LGPD.

## 3. Jornadas de usuário
- Administrador Valora gerencia plataforma global, planos, módulos, clientes, logs, backup e financeiro.
- Consultor Valora acompanha operação e clientes conforme escopo autorizado, sem alterar regras financeiras críticas.
- Empresa Admin administra apenas sua organização, seus usuários, pesquisas, formulários, convites, relatórios e consumo do plano.
- Gestor de Pesquisa cria formulários/pesquisas, publica, envia convites e acompanha respostas da própria organização.
- Analista de Resultados consulta respostas, indicadores, relatórios e exportações permitidas.
- Gestor de Área consulta dados filtrados por departamento.
- Participante responde pesquisas e acessa resultados/certificados próprios quando liberado.
- Convidado Externo responde por link/token, sem acesso administrativo.

## 4. Perfis e permissões
Perfis obrigatórios: `admin_valora`, `consultor_valora`, `empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `gestor_area`, `participante` e `convidado_externo`. As permissões legadas mapeadas são: `canAccessPortal`, `canManageCompanies`, `canManagePlans`, `canManageModules`, `canManageGlobalSettings`, `canManageCompanyUsers`, `canCreateForms`, `canCreateSurveys`, `canSendInvites`, `canViewResponses`, `canViewReports`, `canViewFinance`, `canViewLogs`, `canBackup`, `canAnswerSurveys`, `canHandleSupport`, `canHandleGlobalSupport` e `restrictedToDepartment`.

## 5. Regras de plano e limite
Planos iniciais: Gratuito, Essencial, Growth e Enterprise. Limites: pesquisas ativas, respostas/mês, gestores, funcionários e e-mails/mês. O backend deve bloquear criação/envio quando o limite efetivo do plano e dos overrides da organização for excedido. Módulos comerciais dependem do plano contratado e de habilitação por organização.

## 6. Entidades de dados
A nova modelagem cobre Organization, OrganizationBranding, OrganizationSettings, OrganizationModule, Subscription, Plan, PlanLimit, PlanCapability, User, UserProfile, Role, Permission, RolePermission, UserSession, PasswordResetToken, AccessPolicy, Unit, Department, Employee, Participant, Form, FormDimension, Question, QuestionOption, FormVersion, Survey, SurveyLink, SurveyInvite, SurveyParticipant, Response, ResponseAnswer, ResultScore, DimensionScore, ResultRecommendation, Certificate, CertificateValidation, Communication, EmailTemplate, EmailJob, WhatsAppJob, Notification, AuditLog, OperationalLog, LgpdConsent, PrivacyRequest, DataExportRequest, SupportTicket, SupportTicketMessage, SystemEvent, MigrationBatch e ImportLog.

## 7. Coleções Firebase/localStorage para PostgreSQL
- `companies`/`organizations` -> `organizations`, `organization_branding`, `organization_settings`.
- `users` -> `users`, `user_profiles`, `roles`, `permissions`.
- `plans`/`modules` -> `plans`, `plan_limits`, `plan_capabilities`, `organization_modules`.
- `forms` -> `forms`, `form_dimensions`, `questions`, `question_options`, `form_versions`.
- `surveys`/`surveyLinks`/`invites` -> `surveys`, `survey_links`, `survey_invites`, `survey_participants`.
- `responses`/`results` -> `responses`, `response_answers`, `result_scores`, `dimension_scores`, `result_recommendations`.
- `certificates` -> `certificates`, `certificate_validations`.
- `emailJobs`/`communications` -> `communications`, `email_templates`, `email_jobs`, `whatsapp_jobs`, `notifications`.
- `auditLogs`/`logs` -> `audit_logs`, `operational_logs`, `system_events`.
- `lgpdConsents`/`privacyRequests` -> `lgpd_consents`, `privacy_requests`, `data_export_requests`.
- `migrationBatches`/diagnostics -> `migration_batches`, `import_logs`.

## 8. Cloud Functions para endpoints/services .NET
- Validação/carregamento de pesquisa pública -> `PublicSurveysController` + serviços de jornada pública.
- Envio de resposta pública -> serviço transacional de resposta.
- Cálculo de resultado -> `SurveyResultCalculator`, `QuestionScoreCalculator`, `DimensionScoreCalculator`, `ResultBandResolver`.
- Envio de resultado por e-mail -> fila `EmailJobService`.
- Certificado público -> `CertificateService` e endpoints de validação/download.
- Auditoria/log server event -> `AuditRepository` e middleware de correlationId.
- Migração/importação -> `MigrationService`, `LegacyImportService`, `LegacyMappingService`, `MigrationRepository`.

## 9. Regras de cálculo de resultado
O cálculo deve suportar escala 1 a 5, escolha única, múltipla escolha, texto com pontuação por preenchimento, questão com resposta correta, peso por pergunta, dimensão, nota máxima, percentual, nota normalizada 0–5, faixa de resultado, recomendação e pontuação por dimensão.

## 10. Regras de certificado
Certificado nasce de resposta elegível, usa código público de validação com hash persistido, payload seguro sem hashes expostos, validação pública e contrato para PDF/download. PDF rico pode evoluir em etapa posterior, mantendo contrato limpo.

## 11. Regras de e-mail
SMTP fica no backend por configuração/secret, senha nunca vai ao frontend, envios entram em `email_jobs`, possuem tentativas, dead letter, status, template e auditoria. Casos: convite, resultado, recuperação de senha e teste administrativo seguro.

## 12. Regras de LGPD
Consentimento explícito na jornada pública quando obrigatório, trilha de auditoria, exportação, anonimização e exclusão como solicitações rastreáveis. Logs e respostas públicas não podem expor documento, telefone, e-mail completo, token ou hash.

## 13. Regras de auditoria
Eventos sensíveis: login, falha de login, criação de usuário, alteração de perfil, criação de organização, alteração de plano, criação de formulário, publicação de pesquisa, geração/revogação de link, envio de resposta, consulta de resultado, certificado, e-mail, LGPD, importação e configurações.

## 14. Migrado agora
- Consolidação da decisão arquitetural em `backend/Valora.sln`.
- Complemento das entidades mínimas de domínio pendentes.
- Contratos de importação legado e mapeamento inicial.
- Calculadoras C# pragmáticas para resultado de pesquisas dinâmicas.
- Teste automatizado cobrindo escala, única, múltipla, texto, peso e dimensão.
- Documentação funcional e auditoria final desta etapa.

## 15. Próxima etapa
- Completar endpoints CRUD ainda sem implementação real.
- Expandir scripts SQL para todas as tabelas novas, mantendo idempotência.
- Implementar repositórios Dapper para entidades adicionadas.
- Adicionar autenticação completa com BCrypt/JWT/refresh token.
- Implementar fila SMTP real e certificado PDF final.
- Executar testes em ambiente com .NET SDK disponível.

## 16. Riscos
- Divergência entre coleções legadas e tabelas novas.
- Dados demo misturados com dados reais.
- Tokens públicos antigos sem hash.
- Regras críticas ainda duplicadas no JavaScript durante transição.
- Ambiente atual sem `dotnet` impede validação compilada nesta execução.

## 17. Rollback
Manter legado intacto e provider atual até cutover. Aplicar migração em batches idempotentes com `migration_batches` e `import_logs`. Em falha, pausar cutover, restaurar backup PostgreSQL, manter Firebase/legado como fonte operacional e reprocessar apenas batches corrigidos.
