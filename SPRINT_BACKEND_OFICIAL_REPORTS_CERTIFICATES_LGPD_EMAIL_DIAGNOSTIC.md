# Diagnóstico inicial — Sprint Backend Oficial Reports/Certificates/LGPD/E-mail

## 1. Resultados no `backend`
O backend oficial já possui entidades e repositórios de respostas/resultados (`Response`, `ResponseAnswer`, `ResultScore`, `IResponseRepository`, `ResultRepository`) e calculadores em `Valora.Application/Results`, usados como base para relatórios reais.

## 2. Certificados no `backend`
Existem fluxos prévios de certificado (`Certificate`, `CertificateService`, controllers públicos/operacionais). A sprint consolidou o fluxo operacional oficial com validação pública, revogação e download HTML seguro.

## 3. Comunicações/e-mail no `backend`
Existem serviços de comunicação/e-mail e fila. A sprint mantém SMTP somente no backend, expõe status sem senha e usa `DevelopmentOutbox` quando SMTP real não estiver configurado.

## 4. Auditoria no `backend`
Existe `AuditLog`/`AuditRepository`. Bloqueios e ações críticas de relatórios, certificados, exportações, LGPD e e-mail devem registrar eventos sem dados sensíveis.

## 5. Legado sobre relatórios
O legado contém regras em `report-service.js`, `analytics-service.js` e `pdf.js`, com agregações por resposta/pesquisa e geração visual. Foi usado somente como referência.

## 6. Legado sobre certificados
O legado contém emissão/validação de certificados em fluxos Firebase/Functions. A migração oficial evita Firebase no Web e centraliza regras no backend .NET.

## 7. Legado sobre exportações
Exportações legadas são baseadas em dados Firebase/JavaScript. A sprint exige exportação via PostgreSQL/Dapper e remoção de senha/hash/token dos payloads.

## 8. Legado sobre LGPD
O legado registra consentimentos e textos em fluxos públicos. A sprint oficializa hash/máscara de e-mail/IP, protocolo público e tratamento administrativo.

## 9. Legado sobre envio de e-mail
O legado usa Functions/serviços JS para envio. A versão oficial usa fila no backend, templates e SMTP por variáveis `VALORA_SMTP_*`.

## 10. Tabelas oficiais existentes
Já existiam organizações, usuários, pesquisas, formulários, perguntas, respostas, resultados, auditoria, planos, módulos, assinaturas, uso, comunicações e certificados base.

## 11. Tabelas oficiais faltantes
Faltavam ou precisavam consolidação: `report_definitions`, `generated_reports`, `certificate_validations`, `export_jobs`, `lgpd_consents`, `privacy_requests`, `email_templates`, `email_jobs` e índices correspondentes.

## 12. Repositories faltantes
Faltavam repositories operacionais oficiais para relatórios, certificado operacional, exportação, LGPD/privacidade, templates/jobs de e-mail, além de SaaS quando não disponíveis.

## 13. Services faltantes
Faltavam services oficiais para entitlements/menu, relatórios, builder de relatórios, certificados/validação, exportações, LGPD, privacidade, fila/status/envio de e-mail.

## 14. Controllers faltantes
Faltavam controllers/rotas oficiais para Reports, Certificates, Public Certificates, Exports, LGPD público/admin e E-mail operacional.

## 15. Telas Web faltantes
Faltavam telas MVC/Razor para relatórios, certificados/validação, exportações, LGPD/privacidade, solicitações LGPD, templates, fila e status de e-mail.

## 16. Módulos/capabilities que precisam bloquear recursos
`relatorios`, `certificados`, `exportacoes`, `lgpd`, `convites_email` e `auditoria` devem bloquear recursos por assinatura inativa, módulo ausente, limite de plano e permissão.

## 17. Riscos de segurança
Principais riscos: expor hash/token/senha/SMTP password, retornar stack trace, logar e-mail completo ou token público, exportar payload sensível e validar certificado revelando dados pessoais.

## 18. Plano objetivo da sprint
Consolidar entidades/SQL, repositories/services, controllers, Web MVC, menu, auditoria, testes estáticos, validador npm e documentação, sem alterar `backend-v2` nem criar novo frontend.

## Validação de base
- `dotnet build backend/Valora.sln`: não executável neste ambiente porque `dotnet` não está instalado (`/bin/bash: dotnet: command not found`).
- `dotnet test backend/Valora.sln`: não executável pelo mesmo motivo.
- `npm run backend:official-validate`: executado com PASS.
- `npm run backend:reports-email-validate`: executado com PASS após implementação/consolidação.
