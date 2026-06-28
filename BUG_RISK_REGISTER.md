# Bug Risk Register

| Risco | Severidade | Mitigação |
|---|---:|---|
| Ambiente sem Docker/.NET/Playwright impede evidência completa | Média | Rodar release candidate em agente CI preparado |
| Cutover sem aprovação | Alta | Manter DATA_PROVIDER=firebase e ALLOW_API_PRODUCTION_CUTOVER=false |
| Bug funcional não coberto por automação | Média | Bug bash final com checklist e evidências |
