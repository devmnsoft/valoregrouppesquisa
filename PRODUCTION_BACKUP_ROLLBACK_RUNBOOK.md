# Runbook de Backup e Rollback de Produção
1. Como exportar Firestore antes de reparos: executar exportação gerenciada para bucket controlado e registrar caminho.
2. Como baixar backup de configuração: salvar config.js, firebase.json, .firebaserc, firestore.rules e variáveis operacionais.
3. Como registrar APP_VERSION atual: copiar valor live e commit/hash antes do deploy.
4. Como fazer rollback do Firebase Hosting: usar histórico de releases do Hosting e restaurar versão anterior.
5. Como fazer rollback das Functions: redeploy do artefato/tag anterior validado ou reverter commit e executar deploy controlado.
6. Como reverter config.js: restaurar backup versionado e invalidar cache.
7. Como desativar temporariamente envio de e-mail: remover/rotacionar Secret SMTP ou ativar flag operacional de bloqueio.
8. Como desativar smoke tests: não definir ALLOW_PRODUCTION_* em produção assistida.
9. Como reativar link gratuito: usar repairFreeSurveyPublicLink com admin_valora após backup.
10. Como comunicar indisponibilidade: publicar aviso, registrar incidente, ETA e conclusão.
