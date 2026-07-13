# Legacy SaaS Consolidation — Next Evolution Audit

| Área | O que já existe | Falha atual | Evolução implementada | Arquivos alterados |
|------|-----------------|-------------|------------------------|--------------------|
| Catálogo de planos | `VALORA_PLAN_CATALOG` no front | Motor não era tratado como fonte oficial em todos os fluxos | Normalização, limites, capabilities e seção por plano consolidados | app.js, functions/index.js |
| Capabilities dos planos | Flags por plano | Algumas telas apenas exibiam recursos | `assertCapabilityOrUpgrade`, `requireCapabilityOrShowUpgrade` e preview premium | app.js |
| Limites de planos | Limites no catálogo | Enforcement parcial | Enforcement server-side para respostas/mês e UX de upgrade | app.js, functions/index.js |
| Onde limites são aplicados no front | Criação e modais existentes | Bloqueios pouco comerciais | Modal premium com plano recomendado e solicitação | app.js, style.css |
| Onde limites são aplicados nas Functions | Validações públicas | Sem motor próprio de plano | Catálogo server-side, `enforceServerPlanLimit`, respostas/mês | functions/index.js |
| Onde limites ainda não são aplicados | Fluxos legados dispersos | Alguns CRUDs dependem da camada client/local | Auditados para próxima etapa de hardening transacional | LEGACY_SAAS_CONSOLIDATION_NEXT_EVOLUTION_AUDIT.md |
| Fluxo de upgrade | CTA e WhatsApp | Não persistia solicitação estruturada | `plan_upgrade_requests` e Function `requestPlanUpgrade` | app.js, firebase-repository.js, repository.js, functions/index.js |
| Modal de bloqueio por plano | Modal simples | Toast seco em alguns cenários | `renderPlanUpgradeRequiredModal` consultivo | app.js, style.css |
| CTA comercial por plano | Texto por plano | Não integrado a e-mail/WhatsApp | CTA em e-mail por plano e mensagem por contexto | app.js, report-service.js |
| Relatório por plano | `getReportSectionsForPlan` inicial | Poucas seções | Matriz v2 com Raio-X, Agenda, 30/60/90, consolidado e comparações | app.js, report-service.js, pdf.js |
| Benchmarking | Benchmark premium inicial | Faltava tensão organizacional | Índices Valora, mapa de tensão, referências qualitativas e disclaimer GPTW seguro | app.js |
| Raio-X executivo | Já existia | Gating por plano incompleto | `showExecutiveXRay` por `executiveReport` | app.js |
| Diferenciais Valora | Diferenciais iniciais | Menos comercial | Seção “Por que esta leitura é diferente” mantida e validada | app.js |
| WhatsApp | Links de pesquisa/resultado | Mensagem pouco contextual | `buildWhatsappMessageByContext` por contexto/plano | app.js |
| E-mail | Reenvio e templates | Não variava por plano | `buildResultEmailByPlan` | app.js, report-service.js |
| Resultado público por token | Já protegido | Observabilidade dispersa | Saúde do sistema lista erros e regeneração | app.js |
| Pesquisa pública por token | Já protegida | Limite de plano parcial | Function aplica limite mensal na submissão | functions/index.js |
| Mobile | Layout responsivo existente | Novas áreas faltavam cards | Cards/tabelas reutilizam classes responsivas | app.js, style.css |
| Notificações | Central existente | Não conectava upgrades | Painel de saúde do sistema mostra upgrades | app.js |
| Histórico de evolução | Métricas dispersas | Sem rota dedicada | `#admin/evolution-history` | app.js |
| Comparação entre unidades | Dados em respostas | Sem rota dedicada | `#admin/unit-comparison` com entitlement | app.js |
| Plano de ação | Já existia | Preview por plano ausente | 30/60/90 e gating por plano | app.js, pdf.js |
| Painel de saúde organizacional | Métricas em dashboards | Sem visão consultiva dedicada | `#admin/organizational-health` | app.js |
| O que ainda falta para o produto ficar consolidado | Base sólida | Alguns CRUDs locais dependem de migração transacional completa | Próximo passo: mover todos os CRUDs de criação/ativação para Functions com transações e contadores mensais oficiais | Documento de auditoria |
