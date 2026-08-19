# Mapa de rotas API, BFF e Web

A validação dinâmica está pendente porque o SDK .NET não está instalado neste ambiente. O mapeamento deve ser reconciliado com controllers e scripts antes do smoke.

| Web | BFF/API esperado | Consumidor | Status / pendência |
|---|---|---|---|
| `/Login`, `/Dashboard` | auth, dashboard | views/scripts correspondentes | pendente de startup/login |
| `/Organization`, `/Users`, `/Plans` | endpoints administrativos | páginas administrativas | pendente de tenant/RBAC/entitlement |
| `/Diagnostics`, `/Diagnostics/New`, `/Diagnostics/{id}`, `/Diagnostics/{id}/Workspace` | diagnostics/workspace | páginas de diagnóstico | pendente de fluxo completo |
| `/PublicSurvey/{token}`, `/PublicResults/{token}` | endpoints públicos por token | formulário/resultado público | pendente LGPD/resposta |
| `/Certificates`, `/Certificates/Validate/{code}` | certificates/validation | preview e validação | pendente de smoke |
| `/ExecutiveReports` | executive reports | preview/exportação | pendente de smoke |
| `/Notifications`, `/PlatformGovernance`, `/Audit`, `/SystemHealth` | operação/governança | páginas e topbar | saúde depende de startup |
| `/Intelligence/{Evidence,Metrics,Indices,Inference,Insights,Action,Evolution,Journey,Heatmap,Radar,Benchmark}` | intelligence | páginas/scripts do módulo | pendente de dados reais |
| `/Integrations`, `/Integrations/{ApiKeys,Webhooks,PowerBI}`, `/OneOnOne`, `/Imports` | integrações Enterprise | páginas/scripts Enterprise | pendente de entitlement/configuração real |

**Critério:** cada página com JavaScript deve apontar para BFF real; cada BFF deve alcançar API/service real. Ausência de configuração deve renderizar “Este recurso ainda não está configurado neste ambiente.”, nunca 404 ou botão morto.
