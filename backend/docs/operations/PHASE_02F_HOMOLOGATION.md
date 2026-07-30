# Homologação da Fase 02F

1. Aplicar `script_completo.sql` duas vezes em banco vazio.
2. Aplicar migrations em ordem, incluindo `20260730_005_identity_sessions_bff_completion.sql`.
3. Executar restore, build, testes e format de `Valora.sln`.
4. Validar login e confirmar registros de sessão, família e hash do refresh.
5. Renovar, reapresentar o token antigo e confirmar revogação da família e sessão.
6. Validar logout, logout global, isolamento por usuário e RBAC persistido.
7. Em múltiplas instâncias, configurar Redis e key ring compartilhado; não usar memória distribuída em produção.

Rollback operacional: revogar sessões ativas e reimplantar a versão anterior. A migration é aditiva e não utiliza `DROP TABLE`.
