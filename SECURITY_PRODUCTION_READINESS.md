# SECURITY PRODUCTION READINESS

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

## Sprint 28 - Homologação funcional SaaS

- Produção permanece com Firebase preservado e `DATA_PROVIDER=firebase`; cutover para API exige aprovação e flag explícita.
- PostgreSQL local usa exclusivamente o schema `valorapesquisa`.
- Gates finais reforçados: SaaS readiness, certificado, e-mail, billing, cobertura frontend, E2E SaaS, cutover dry-run, rollback readiness e release candidate com relatório.
- Limitação conhecida: ambientes sem API/PostgreSQL ativos executam validação estática/contratual; validação viva é feita com `VALORA_E2E_LIVE=1 npm run prod:saas-e2e`.

## Sprint 30 - Gate executável de segurança

O gate `prod:security-gate` valida controles de código e configuração antes da homologação final:

- JWT com issuer, audience, lifetime e chave de assinatura configurável por ambiente.
- Middleware de erro JSON sem stack trace em produção e com `correlationId`.
- Headers de segurança no Firebase Hosting, incluindo CSP, X-Frame-Options e X-Content-Type-Options.
- Regras Firestore preservadas com autenticação/negação explícita.
- Endpoints administrativos protegidos por `[Authorize]`.
- Rotas legacy bloqueadas por `LegacyEnabled` e `legacy_route_disabled`.
- `DATA_PROVIDER=firebase` e `ALLOW_API_PRODUCTION_CUTOVER=false` preservados.
- Política de CORS por ambiente: liberar somente origens homologadas em produção e manter localhost apenas para desenvolvimento.
- Rate limit obrigatório para autenticação e pesquisa pública antes de exposição externa da API.
- Limite de payload obrigatório para submissões públicas para evitar abuso e custo operacional.
- Proibição de stack trace, segredos e dados pessoais completos em respostas/logs.
