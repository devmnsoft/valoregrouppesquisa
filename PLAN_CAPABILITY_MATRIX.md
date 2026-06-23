# Matriz de aderência dos planos

Auditoria inicial baseada no código atual. Critério: texto comercial isolado não conta como implementação completa.

| Plano | Promessa pública | Tipo | Evidência técnica | Status | Risco | Ação necessária |
|---|---|---:|---|---|---|---|
| Grátis | 1 pesquisa ativa | software | `maxActiveSurveys`, `usageBars`, criação de pesquisas usa limites parciais | parcial | guardas client-side | teste contratual e bloqueio central |
| Grátis | Até 10 respostas | software | `maxResponsesMonth`, `responsesThisMonth` | parcial | janela mensal simples | motor central de consumo |
| Grátis | Resultado individual | software | renderização de resultado e cálculo | implementado | acesso por token deve ser revisado | teste de permissão |
| Grátis | Devolutiva resumida | software | bandas e resultado | parcial | definição de conteúdo não centralizada | contrato de relatório resumido |
| Grátis | Certificado simples | software | módulo certificados/resultado | parcial | evidência visual/PDF dispersa | teste PDF/PNG |
| Essencial | 3 pesquisas ativas | software | `maxActiveSurveys:3` | parcial | bloqueio não centralizado | entitlement |
| Essencial | Até 150 respostas/mês | software | `maxResponsesMonth:150` | parcial | concorrência/backend | consumo mensal |
| Essencial | 2 gestores | software | `maxManagers:2` | parcial | perfis contáveis não formalizados | regra de gestor |
| Essencial | Devolutiva estratégica | software | texto e relatórios | parcial | pode prometer além do plano | separar níveis |
| Essencial | Relatório básico | software | módulo relatórios genérico | parcial | formato básico indefinido | template básico |
| Profissional | 12 pesquisas ativas | software | `maxActiveSurveys:12` | parcial | idem | entitlement |
| Profissional | Até 1.000 respostas/mês | software | `maxResponsesMonth:1000` | parcial | idem | consumo |
| Profissional | 8 gestores | software | `maxManagers:8` | parcial | idem | guardas |
| Profissional | Relatório executivo | software | relatório estratégico/PDF | parcial | nível não parametrizado | capability `executiveReport` |
| Profissional | Plano de ação | software | `actionPlans` e módulo | implementado parcial | permissão por plano dispersa | entitlement |
| Corporativo | Pesquisas ilimitadas | software | `maxActiveSurveys:-1` | parcial | -1 não auditado em todos pontos | normalizador |
| Corporativo | Até 10.000 respostas/mês | software | `maxResponsesMonth:10000` | parcial | backend ausente | consumo |
| Corporativo | 50 gestores | software | `maxManagers:50` | parcial | idem | guardas |
| Corporativo | Múltiplas unidades | software | não há entidade robusta `units` validada | ausente | promessa pública sem entrega plena | implementar/remover da vitrine |
| Corporativo | Relatórios consolidados | software | relatórios agregados genéricos | parcial | unidade/período/anonimização | relatório consolidado |
| Enterprise | Ambiente personalizado | híbrido | branding existente, domínio ausente | parcial | escopo comercial ambíguo | separar branding/domínio |
| Enterprise | Múltiplas empresas | software | `maxCompanies:-1`, sem contrato multiempresa completo | parcial | isolamento/consolidação | modelagem |
| Enterprise | Reunião estratégica | serviço humano | não é automático | apenas texto/serviço | sem tracking | `serviceDeliverables` |
| Enterprise | Relatórios consultivos | serviço humano | não é automático | apenas texto/serviço | sem tracking | `serviceDeliverables` |
| Enterprise | Acompanhamento executivo | serviço humano | não é automático | apenas texto/serviço | sem tracking | `serviceDeliverables` |

## Gaps críticos ordenados
1. Fonte única de capacidades e limites.
2. Guardas de limite centralizados.
3. Múltiplas unidades antes de anúncio público pleno.
4. Rastreio de serviços Enterprise.
5. Testes contratuais por plano.
