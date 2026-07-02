# Plano de cutover controlado — Valora backend oficial

1. Pré-requisitos: build/test/validadores, backup testado, dry-run sem conflito bloqueante, checklist assinado.
2. Responsáveis: tecnologia, negócio, suporte, comunicação e aprovador executivo.
3. Janela de mudança: definida com início, fim e canal de war room.
4. Congelamento do legado: legado preservado e colocado em modo somente leitura quando aprovado.
5. Backup antes do cutover: executar runbook e validar dump.
6. Exportação do legado: exportar dados sanitizando evidências públicas.
7. Dry-run final: obrigatório antes de qualquer apply.
8. Relatório de divergências: revisar conflitos, contagens e amostras.
9. Aprovação: confirmação explícita registrada.
10. Apply final: executar batch aprovado, sem automação de DNS.
11. Conciliação: comparar contagens e amostras críticas.
12. Readiness: gerar `/migration/batches/{id}/cutover-readiness`.
13. Rota/DNS/proxy: alterar apenas manualmente por responsável autorizado.
14. Monitoramento pós-cutover: health, logs, e-mail, respostas públicas e suporte.
15. Critérios de sucesso: fluxos críticos OK e sem divergência bloqueante.
16. Critérios de rollback: erro crítico, perda de dados, indisponibilidade prolongada ou falha de conciliação.
17. Comunicação aos usuários: aviso antes, durante e depois.
18. Suporte pós-cutover: plantão, triagem e hotfix controlado.

Cutover não deve ser executado nesta sprint.
