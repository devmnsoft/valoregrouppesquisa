# Bug Risk Register

| Risco | Impacto | Mitigação | Responsável | Tempo estimado |
|---|---|---|---|---|
| API local sem fixture de pesquisa | Bloqueia SaaS E2E live | Informar VALORA_E2E_SURVEY_ID ou criar seed local | Engenharia | 2h |
| Rate limit não comprovado em ambiente local-prod | Risco de abuso | Security live documenta bloqueio e deve falhar quando política exigir | Segurança | 4h |
| Cutover com fixture não cobre Firestore real | Divergência de dados | Usar --live-firebase-confirmed apenas em janela aprovada | Engenharia/DPO | 1 dia |
| Rollback não exercitado em Windows real | Risco operacional | Executar scripts BAT em homologação | Operações | 2h |
