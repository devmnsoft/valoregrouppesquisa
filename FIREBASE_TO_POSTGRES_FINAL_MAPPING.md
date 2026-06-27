# FIREBASE TO POSTGRES FINAL MAPPING

Sprint 27 — diagnóstico de prontidão SaaS para homologação.

## Diagnóstico real
- Firebase permanece como provedor seguro de produção até cutover aprovado.
- API/PostgreSQL está tratada como caminho homologável/controlado, com gates adicionais antes de publicação.
- Rotas legacy devem permanecer desabilitadas fora de Development, salvo configuração explícita temporária.
- Forgot/reset password usa token com hash, expiração, fila de e-mail e auditoria; token puro não deve ser logado nem persistido.

## Pronto
- Cadastro de empresa com plano free.
- Login JWT e /me.
- Pesquisa pública, submissão, resultado e certificado por API.
- Health checks, logging técnico, correlationId, migration runner e scripts de validação.

## Incompleto / não publicar sem homologação
- Cutover Firebase → PostgreSQL depende de dry-run real, comparação aprovada, janela operacional e rollback testado.
- Pagamentos/checkout externo permanecem preparados, não automatizados.
- Relatórios avançados e governança completa LGPD exigem validação jurídica/operacional.

## Gates obrigatórios
- npm run prod:saas-readiness
- npm run prod:no-legacy
- npm run prod:no-pending
- npm run prod:auth-flow
- npm run prod:certificate-flow
- npm run prod:email-flow
- npm run prod:billing
- npm run prod:security-gate
- npm run prod:final-gate
