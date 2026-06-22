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
