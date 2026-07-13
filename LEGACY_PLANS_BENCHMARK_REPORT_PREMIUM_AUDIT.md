# Auditoria premium — planos, limites, benchmarking e relatório Valora Insight™

| Item | Onde está hoje | Problema | Correção aplicada | Arquivos alterados |
|---|---|---|---|---|
| 1. Onde os planos são definidos | `app.js`, seeds e docs comerciais | Definições espalhadas e divergentes | Criado `VALORA_PLAN_CATALOG` como fonte única e adaptadores legados | `app.js`, `package.json` |
| 2. Onde os preços são definidos | Cards públicos e fallback legado | Preço não seguia catálogo único | Preços centralizados no catálogo | `app.js` |
| 3. Recursos/capabilities | Arrays e flags antigas | Recursos pouco auditáveis | Capabilities booleanas por plano | `app.js` |
| 4. Limites | `maxActiveSurveys`, `maxResponsesMonth`, `maxManagers` | Nomes técnicos e limites incompletos | `limits` com pesquisas, respostas, gestores, clientes, empresas e unidades | `app.js` |
| 5. Limite de pesquisas | `canCreateSurvey`, `limitAvailable` | Bloqueio seco | `enforcePlanLimit` polimórfico e modal premium | `app.js` |
| 6. Limite de respostas/mês | `canCollectResponse`, `getCompanyUsage` | Sem mensagem comercial clara | Limite parametrizado no catálogo | `app.js` |
| 7. Limite de gestores | `canAddManager` | Preso a `maxManagers` | Mapeado para `limits.managers` | `app.js` |
| 8. Limite de clientes/empresas | Seeds e plano Enterprise | Inconsistente | `clients` e `companies` no catálogo | `app.js` |
| 9. Diferenciação dos seis planos | Faltava Growth na tela pública oficial | Grade antiga tinha cinco planos | Adicionado Growth e matriz com seis planos | `app.js` |
| 10. “Certificado simples” | Seed e capabilities antigas | Recurso removido do produto atual | Texto e capability removidos/substituídos por acesso seguro ao resultado | `app.js`, `firestore.seed.sample.json` |
| 11. Upgrade de plano | `openPlanUpgradeModal` | Modal genérico | Modal premium com plano recomendado e benefícios | `app.js` |
| 12. CTA comercial | Home, planos e relatório | CTAs pouco segmentados | `getCommercialCtaByPlan` por plano | `app.js` |
| 13. Bloqueio por plano | `enforcePlanLimit` antigo | Toast seco | `renderPlanLimitModal` | `app.js` |
| 14. Grátis para pago | Cards de planos | Sem evolução clara | CTA “Evoluir plano” e solicitação comercial | `app.js` |
| 15. Relatório por plano | Relatório único | Não havia matriz explícita | `getReportSectionsForPlan` | `app.js` |
| 16. Benchmarking | `buildStructuralBenchmarking` | Bom, mas não suficientemente comercial | `buildValoraMarketBenchmark` premium | `app.js` |
| 17. GPTW | Benchmarking qualitativo | Disclaimer precisava ficar obrigatório | Disclaimer seguro e GPTW Brasil como referência conceitual | `app.js` |
| 18. Índices Valora | `buildValoraIndexes` | Existia parcialmente | Validado com índices de maturidade, confiança, governança e gap | `app.js` |
| 19. PDF | `generateValoraInsightReportPdf` | PDF consome seções existentes | Benchmarking premium e Raio-X disponíveis no modelo | `app.js` |
| 20. HTML | `renderValoraInsightResultPage` | Faltavam X-Ray e diferenciais | Inseridos “Raio-X executivo Valora” e “Diferenciais Valora Insight™” | `app.js` |
| 21. Mobile | CSS de radar e cards | Risco em grids/tabelas | Matriz usa table-wrap; cards usam grid responsivo existente | `app.js`, `style.css` |
| 22. Validadores existentes | `scripts/validate-*.js` | Cobriam partes legadas | Mantidos e complementados | `scripts/*.js` |
| 23. Validadores criados | Não existiam todos os gates pedidos | Sem cobertura premium completa | Criados 12 validadores e scripts npm | `scripts/validate-legacy-*.js`, `package.json` |
