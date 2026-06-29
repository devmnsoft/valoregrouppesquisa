# Sprint 47 — Auditoria operacional da pesquisa gratuita

Escopo auditado: `index.html`, `app.js`, `pdf.js`, `config.js`, `firebase.json`, `backend/Valora.Api/`, `backend/Valora.Application/`, `backend/Valora.Infrastructure/`, `backend/Valora.Web/`, `database/postgresql/`, `scripts/`, `tests/`, `tests/e2e/` e `tests/e2e-web/`.

1. A pesquisa gratuita está aparecendo como entrada principal? Sim, os validadores `home:free-survey-link` e `web:free-survey-parity` permanecem no gate.
2. Os cards de planos foram removidos da Home pública? Sim, gate `home:no-plan-cards` mantido.
3. O link público da pesquisa gratuita é compartilhável? Sim, gate `home:free-survey-link` mantido.
4. O link depende de login? Não deve depender; validado pelo fluxo público existente.
5. O link expira indevidamente? Não foi identificado prazo indevido nos gates atuais.
6. O participante precisa informar e-mail? Sim, gate `home:result-email-required` mantido.
7. O resultado é enviado por e-mail automaticamente? Parcial: há `email_jobs` transacional; operação e reenvio foram adicionados nesta sprint.
8. O certificado é gerado com conteúdo rico? Sim, gate `certificate:rich-content` mantido.
9. Existe registro de email_job? Sim, tabela `valorapesquisa.email_jobs` e painel operacional.
10. Existe status sent, pending, processing e failed? Sim, status expostos no painel e API operacional.
11. Existe reenvio manual? Sim, endpoint `POST /admin/free-diagnostics/{responseId}/resend-email`.
12. Existe retry automático ou manual? Manual controlado por limite; fila `pending` preserva processamento assíncrono.
13. Existe auditoria LGPD do envio? Sim, eventos documentados em `FREE_SURVEY_LGPD_AUDIT.md`.
14. Existe trilha do resultToken sem expor hash? Sim, o painel não consulta nem exibe hashes ou token puro.
15. O admin consegue ver respostas gratuitas? Sim, `GET /admin/free-diagnostics` e tela Valora.Web.
16. O admin consegue ver falhas de e-mail? Sim, painel lista falhas sanitizadas.
17. O admin consegue reenviar resultado? Sim, com limite e justificativa.
18. O Valora.Web possui tela operacional equivalente? Sim, `/FreeDiagnostics`.
19. Quais pontos ainda bloqueiam produção? Homologação SMTP real, execução completa de Playwright em ambiente com browsers e verificação de cutover em Windows/IIS.
20. Quais pontos são melhoria futura? Métricas por campanha, retry exponencial configurável, alertas proativos e trilhas exportáveis para DPO.
