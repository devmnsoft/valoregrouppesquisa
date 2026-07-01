# Diagnóstico inicial — Sprint Backend Oficial SaaS + Relatórios + Certificados + LGPD + E-mail

## 1. Gaps ainda existentes
A solução oficial já contém base de planos, pesquisas, respostas, resultados, certificados simples, comunicação e auditoria. Permaneciam gaps de repositories/services reais para módulos, assinaturas, uso mensal, dashboard e menu dinâmico; relatórios, exportações, LGPD e e-mail operacional ainda não possuíam contrato oficial completo.

## 2. Ocorrências de `WEB_ADMIN_REAL_REPOSITORY_REQUIRED`
A busca estática encontrou o marcador apenas em documentação/validador, não em controller oficial executável. O gap deve permanecer documentado enquanto houver endpoints sem repository completo.

## 3. Repositories oficiais existentes
Existem repositories reais para organização, usuários, planos, formulários, pesquisas, respostas, resultados, certificados, comunicação, auditoria, diagnósticos gratuitos e migração.

## 4. Repositories a completar/criar
Criar/completar `ModuleRepository`, `SubscriptionRepository`, `UsageRepository`, `ReportRepository`, `CertificateOperationalRepository`, `ExportRepository`, `LgpdRepository`, `EmailOperationalRepository`, `DashboardMetricsRepository` e suporte de menu quando necessário.

## 5. Services interface/stub
Interfaces como `ICertificateService`, `IEmailJobService`, `IAuditService`, `IValoraInsightCalculator` e `IValoraInsightDevolutivaService` estavam vazias ou sem contrato rico. Services SaaS de módulos/assinatura/uso/dashboard/menu/permission precisavam implementação oficial.

## 6. Controllers a completar
Criar/completar controllers oficiais de reports, certificates, public certificate validation, exports, LGPD e email, além de endpoints auxiliares de envio de resultado/certificado/convite.

## 7. Telas Web com fallback
A Web MVC possuía certificados e comunicações básicas; relatórios, exportações, LGPD, templates/fila/status de e-mail e validação pública precisavam telas dedicadas ou mais explícitas.

## 8. Tabelas SaaS existentes
Scripts oficiais já possuem organizações, usuários, planos/limites/capabilities, pesquisas, respostas, resultados, certificados básicos, comunicação/e-mail e auditoria.

## 9. Tabelas faltantes
Faltavam modelos oficiais consolidados para `report_definitions`, `generated_reports`, `certificate_validations`, `export_jobs`, `lgpd_consents`, `privacy_requests`, `email_templates` e ampliação de `email_jobs`.

## 10. Reaproveitamento conceitual de backend-v2
Apenas conceitos de SaaS, módulos, planos, entitlement e relatórios podem orientar nomes e regras; nenhuma feature deve ser implementada em `backend-v2`.

## 11. O que não reaproveitar
Não reaproveitar estrutura paralela, solution alternativa, dados fake, fallback de UI como segurança nem qualquer fluxo que exponha token/hash/senha.

## 12. Regras do legado migradas nesta sprint
Menu por módulo, bloqueio por plano/módulo/assinatura, relatórios HTML/JSON/CSV, certificado com validação pública, exportações seguras, consentimento LGPD, solicitação de privacidade e fila de e-mail.

## 13. Riscos de segurança
Riscos principais: vazamento de e-mail completo, hashes/tokens, payload_json sensível, segredo SMTP, stack trace e exportações irrestritas. Mitigações: DTOs seguros, mascaramento/hash, entitlements, auditoria e validators.

## 14. Plano objetivo
1. Validar base oficial; `dotnet` indisponível no ambiente.
2. Implementar contracts/entities/DTOs/services/repositories oficiais.
3. Criar endpoints e views MVC.
4. Consolidar SQL e seeds idempotentes.
5. Atualizar documentação e validadores.
6. Executar validações possíveis e documentar limitações.
