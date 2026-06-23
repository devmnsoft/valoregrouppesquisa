# Relatório de migração de planos

## IDs legados
- `growth` deve migrar para `professional`.
- `enterprise` legado com 10.000 respostas/mês deve migrar para `corporate`.
- O novo `enterprise` deve representar oferta consultiva sob consulta.

## Campos a migrar
- `organizations.planId` / `companies.planId`.
- `organizations.subscription.planId` / `companies.subscription.planId`.
- Faturas, assinaturas e overrides devem preservar `legacyPlanId` para auditoria.

## Ordem segura
1. Backup.
2. Dry-run com contagem por plano.
3. Aplicar mapeamento idempotente.
4. Validar Home e contratos.
5. Remover claims públicas sem evidência ou marcar como serviço consultivo.
