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
