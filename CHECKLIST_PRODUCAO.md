# Checklist de Produção — Firebase e Segurança

Use esta checklist como bloqueio de go-live. Cada item deve ter responsável, data e evidência anexada ao registro de release.

## Firebase

- [ ] Firebase Auth ativo no projeto de produção.
- [ ] MFA admin configurado para contas `admin_valora` e operadores críticos.
- [ ] Firestore Rules publicadas e revisadas contra `firestore.rules` versionado.
- [ ] Cloud Functions publicadas a partir da versão aprovada.
- [ ] Secrets configurados no Firebase/Google Cloud Secret Manager, incluindo SMTP e integrações externas.
- [ ] Índices Firestore necessários publicados.

## Hosting e frontend

- [ ] Firebase Hosting publicado com headers de segurança.
- [ ] CSP validada em desktop e mobile sem violações críticas no console.
- [ ] `server.py` não publicado no Hosting nem usado como backend público de produção.
- [ ] `localStorage` não usado como fonte de autenticação/autorização; autenticação deve vir do Firebase Auth e perfil autorizado no Firestore/claims.
- [ ] Logout limpa estado visual e dados em memória da sessão.

## Operação e dados

- [ ] Backup Firestore configurado e com restauração testada ou procedimento documentado.
- [ ] Logs/auditoria ativos para login, envio de resposta, validação de link, alterações administrativas e envio de e-mail.
- [ ] Alertas configurados para erros de Functions, picos de negação e limites de cota.
- [ ] Plano de rotação de secrets definido.

## Validação final

- [ ] `npm run test:security` aprovado contra emuladores.
- [ ] Teste multiempresa aprovado: Empresa A não lê dados da Empresa B em usuários, pesquisas, respostas, invoices e organizações.
- [ ] Jornada pública aprovada: validação de link, envio de resposta e acesso ao resultado por token.
- [ ] Jornada admin aprovada: criação de empresa, criação de usuário, alteração de plano, logs e leitura global.
- [ ] Jornada participante aprovada: perfil próprio, edição restrita e histórico próprio.
- [ ] Exportações validadas sem vazamento entre empresas.
- [ ] Evidências anexadas em `TESTES_EXECUTADOS.md` ou no registro de release.

## Checklist comercial SaaS 2026
- [ ] Publicar `role-definitions.js` e validar menus/botões por perfil.
- [ ] Publicar `module-definitions.js` e validar módulos por plano.
- [ ] Migrar `companies` local para `organizations` no Firestore.
- [ ] Garantir Cloud Function para criação pública de respostas.
- [ ] Garantir Cloud Function para envio de e-mail e atualização de `invitations`.
- [ ] Criar índices por `companyId`, `surveyId`, `status`, `department` e mês.
- [ ] Bloquear empresa `suspended`/`overdue` em criação de pesquisas e envios.
- [ ] Monitorar limites de plano no backend.

## Repository Firestore real

- [ ] `config.js` publicado com `STORAGE_MODE: 'firebase'` e `FIREBASE_ENABLED: true` somente no ambiente Firebase.
- [ ] `firestore.seed.sample.json` convertido em seed real via Admin SDK sem senhas nem tokens puros.
- [ ] Primeiro `admin_valora` criado no Auth, em `users/{uid}` e em custom claims.
- [ ] Primeira organização criada em `organizations` e não em `companies`.
- [ ] Usuário `empresa_admin` criado com `companyId` da organização e claims coerentes.
- [ ] Testado que `state.companies` é alimentado a partir de `organizations`.
- [ ] Confirmado que logs não são gravados diretamente pelo frontend.
- [ ] Confirmado que criação pública de respostas permanece exclusiva de Cloud Functions.

## Primeiro acesso — passo a passo obrigatório

1. Criar projeto Firebase de produção.
2. Ativar Authentication por e-mail/senha.
3. Criar usuário `admin_valora` no Firebase Auth, com senha definida somente no console/Auth ou fluxo seguro interno.
4. Criar `users/{uid}` com `role: "admin_valora"`, `companyId: ""`, `status: "active"`.
5. Definir custom claims do admin com Admin SDK.
6. Publicar Firestore Rules: `firebase deploy --only firestore:rules`.
7. Publicar Cloud Functions: `firebase deploy --only functions`.
8. Publicar Hosting: `firebase deploy --only hosting`.
9. Configurar SMTP via Secret Manager (`SMTP_PASSWORD`) e variáveis `SMTP_HOST`, `SMTP_PORT`, `SMTP_SECURITY`, `SMTP_USERNAME`, `SMTP_SENDER_EMAIL`, `SMTP_SENDER_NAME`.
10. Definir `STORAGE_MODE: 'firebase'` e `FIREBASE_ENABLED: true` no build/ambiente correto.
11. Rodar o seed mínimo de `firestore.seed.sample.json` via Admin SDK.
12. Testar login do admin, dashboard global, criação da primeira empresa real, criação do admin da empresa e envio de convite real.

## Homologação ponta a ponta Valora Pulse

- [ ] Admin Valora acessa dashboard global, clientes, planos, módulos, formulários globais, pesquisas, respostas, relatórios e auditoria.
- [ ] Empresa Admin vê somente a própria empresa, cadastra funcionários, cria formulário/pesquisa, envia convites e consulta uso do plano.
- [ ] Gestor de Pesquisa cria formulários/pesquisas e envia convites sem acessar financeiro global, backup, plano ou perfis Valora.
- [ ] Analista de Resultados tem leitura de dashboards, respostas e relatórios, sem criar formulário/pesquisa ou alterar usuários.
- [ ] Gestor de Área lê somente dados da própria empresa e do próprio `department` quando preenchido.
- [ ] Participante/convidado responde por link seguro sem portal administrativo completo.
- [ ] Dashboards, respostas, relatórios e certificados usam os mesmos campos de resultado (`rawScore`, `maxScore`, `normalized5`, `percentage`, `byDimension`, `level`, `qualitative`).

## Evolução white label e assinatura

Esta versão adiciona estrutura de identidade visual por empresa, slug público, campos de assinatura, limites customizados, status comercial e portal de plano contratado. Consulte `WHITE_LABEL_E_ASSINATURA.md` para modelo, permissões, regras de bloqueio e roteiro de testes.

## Nota CSP Firebase Hosting

- [ ] Confirmar que o header `Content-Security-Policy` publicado mantém `script-src 'self' https://www.gstatic.com` e inclui `https://www.gstatic.com` também em `connect-src`, necessário para recursos/source maps dos SDKs Firebase durante diagnóstico.
- [ ] Confirmar que a CSP continua sem `connect-src *`, sem `script-src *` e sem `unsafe-eval`.

## Checklist de integrações
- [ ] Configurar Cloud Function `publicApi` com domínio/hosting.
- [ ] Definir segredo real de assinatura HMAC para webhooks.
- [ ] Revisar plano Enterprise e limites de rate limit.
- [ ] Validar rules de `integrations`, `apiKeys`, `webhooks` e `integrationLogs` em emulador.
- [ ] Garantir revisão jurídica LGPD para exportações com dados pessoais.


## Observabilidade e Telegram
- [ ] Revisar `observability` por ambiente.
- [ ] Configurar `TELEGRAM_BOT_TOKEN` e `TELEGRAM_CHAT_ID` via Secrets.
- [ ] Validar Firestore Rules para `logs` e `systemLogs`.
- [ ] Testar mascaramento antes de habilitar Telegram em produção.


## Observabilidade e suporte

- Validar `logger-service.js` sem dados sensíveis.
- Configurar Telegram somente via backend/secrets.
- Publicar Cloud Functions de atendimento público com rate limit.
- Revisar regras de `supportConversations` e `supportMessages`.

## CI/CD e deploy controlado

- [ ] PR validada pelo workflow `PR validation`.
- [ ] `dist/` não commitado e validado por `npm run check:no-dist`.
- [ ] Build seguro gerado pelo workflow `Secure production build`.
- [ ] Source maps ausentes no artefato `dist/`.
- [ ] Segredos ausentes no código e no build.
- [ ] Deploy de produção executado somente por `workflow_dispatch` ou tag `v*`.
- [ ] Environment `production` configurado no GitHub com aprovação manual.
- [ ] Rollback revisado conforme `ROLLBACK_PRODUCAO.md`.

## Homologação dos pipelines

- [ ] Evidência anexada de `npm run check` aprovado.
- [ ] Evidência anexada de `npm run check:no-dist` aprovado e simulação de `dist/` bloqueada.
- [ ] Evidência anexada de `npm run security:check` aprovado e simulações de segredo fake/CSP insegura bloqueadas.
- [ ] Evidência anexada de `npm run build:prod`, `node --check dist/assets/app.*.js`, ausência de `.map` e `npm run postbuild:security` aprovados.
- [ ] `git ls-files dist` retorna vazio.
- [ ] Workflow `Firebase production deploy` revisado: somente `workflow_dispatch` ou tag `v*`, com `environment: production`.
- [ ] Deploy real executado apenas após autorização formal, aprovação do environment e plano de rollback conhecido.
- [ ] Resultado da homologação registrado em `HOMOLOGACAO_PIPELINES.md`.

## Firebase PRD/HML — seed inicial

- [ ] Exportar localStorage pelo Admin Valora com sanitização.
- [ ] Conferir que não há senha, SMTP, Telegram, service account ou segredo no JSON.
- [ ] Rodar dry-run do importador Firebase.
- [ ] Rodar `--apply --backup --create-auth-users` somente após aprovação.
- [ ] Validar com `scripts/validate-firebase-seed.js`.
- [ ] Confirmar planos, perguntas, pesquisas, usuários e login em PRD.

## Bootstrap inicial PRD Firebase

Use os scripts de bootstrap somente a partir de uma estação segura com credenciais administrativas via Application Default Credentials ou ambiente controlado de CI/CD. Não versione service accounts, exports, backups ou senhas.

### Dry-run obrigatório

```bash
node scripts/bootstrap-firebase-prd.js --project gestordepesquisa --dry-run
```

O dry-run lista as criações/mesclagens planejadas e não grava no Firebase. Sem `--apply`, o script nunca escreve dados.

### Aplicar bootstrap

```bash
node scripts/bootstrap-firebase-prd.js --project gestordepesquisa --apply --admin-email admin@valoragroup.com.br --admin-name "Admin Valora"
```

Parâmetros principais: `--project`, `--dry-run`, `--apply`, `--admin-email`, `--admin-name`, `--admin-password`, `--seed-demo-survey`, `--seed-demo-response`, `--merge` e `--overwrite`. O padrão é `--merge`; `--overwrite` exige `--confirm-overwrite gestordepesquisa`. A senha nunca é exibida em log. Se `--admin-password` não for informado, uma senha forte temporária é gerada e deve ser substituída por fluxo seguro de redefinição.

O bootstrap cria/atualiza de forma idempotente: Admin Valora em Auth e `users/{uid}`, custom claims `{ role: 'admin_valora', companyId: '' }`, `settings/global`, planos comerciais, módulos, `organizations/org_valora_prd`, compatibilidade em `companies/org_valora_prd`, formulário Valora Insight™, pesquisa ativa com `tokenHash`, base de conhecimento, categorias de atendimento e políticas de SLA. Resposta demo só é criada com `--seed-demo-response`.

### Validar pós-bootstrap

```bash
node scripts/validate-prd-bootstrap.js --project gestordepesquisa
```

A validação confirma Admin em Auth/Firestore, custom claims, planos, módulos, `settings/global`, organização, formulário, pesquisa ativa, vínculo survey→form e referências plan→modules. Depois, validar manualmente no IIS: login admin, portal admin, planos, empresa/plano contratado, perguntas, pesquisa pública, envio de resposta, resultado, certificado e ValoraBot.

### getEmailStatus em PRD

Em modo Firebase, `getEmailStatus` deve continuar sendo chamada por `httpsCallable('getEmailStatus')`, nunca por `fetch` direto para `cloudfunctions.net/getEmailStatus`. Se o e-mail não estiver configurado, a aplicação deve mostrar status amigável e não quebrar o carregamento.

## Checklist Publicador IIS PRD

- Rodar dry-run do publicador.
- Validar Firebase PRD em `config.js`.
- Conferir `dist/index.html`, `dist/assets/app.*.js`, `dist/assets/style.*.css` e `dist/web.config`.
- Confirmar backup em `backups/iis/` antes do `--apply`.
- Validar site no IIS sem 500.19, 403.14 ou MIME `text/html` para JS/CSS.

## Health Check PRD pós-publicação

Use o script `scripts/healthcheck-prd.js` para validar IIS, HTML, assets JS/CSS, MIME, Firebase, Functions, Firestore opcional, pesquisa pública opcional e ValoraBot público após publicar.

```bash
node scripts/healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa --check-firebase --check-functions
```

Integrado ao publicador IIS:

```bash
node scripts/publish-iis-prd.js --iis-path C:\inetpub\wwwroot\valoragroup --mode firebase --apply --health-url https://valoragroup.mnsoft.com.br --project gestordepesquisa
```

Relatórios são gerados em `publish/reports/` e não devem ser commitados.

## Publicador Windows PRD/IIS

- [ ] `publish.config.json` revisado com `firebaseProjectId`, `productionUrl` e `iisPath` corretos.
- [ ] `config.js` em modo Firebase PRD antes do apply.
- [ ] Dry-run executado sem erro.
- [ ] Pacote IIS contém `index.html`, `assets/` e `web.config`.
- [ ] Backup automático criado em `backups/iis/` antes de limpar a pasta IIS.
- [ ] Health check PRD executado sem 500.19, 403.14, asset HTML ou Firebase sem config.
- [ ] Rollback testado com `scripts/restore-iis-backup.js --latest`.
