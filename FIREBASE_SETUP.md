# Firebase Auth e Firestore — configuração de produção

A aplicação mantém `STORAGE_MODE: 'local'` para demonstração. Em produção, altere para `STORAGE_MODE: 'firebase'`, habilite `FIREBASE_ENABLED` e preencha apenas a configuração pública do app Web Firebase em `config.js`.

## Serviços usados

- **Firebase Authentication**: login por e-mail e senha, logout real e listener `onAuthStateChanged`.
- **Cloud Firestore**: perfil operacional em `users/{uid}`.
- **Custom claims**: papel e escopos emitidos pelo backend/Admin SDK quando disponíveis.
- **Firestore Rules e Cloud Functions**: fonte de verdade para autorização. O frontend usa `role` apenas para navegação e exibição de menus.

## Configuração do frontend

```js
window.ValoraConfig = Object.freeze({
  STORAGE_MODE: 'firebase',
  FIREBASE_ENABLED: true,
  FIREBASE_CONFIG: {
    apiKey: '...',
    authDomain: 'seu-projeto.firebaseapp.com',
    projectId: 'seu-projeto',
    storageBucket: 'seu-projeto.appspot.com',
    messagingSenderId: '...',
    appId: '...'
  }
});
```

`firebaseConfig` é público por natureza no SDK Web. Não coloque SMTP, tokens privados, service accounts, segredos de APIs ou credenciais administrativas nesses arquivos.

## Documento obrigatório de usuário

Cada usuário autenticado precisa ter um documento em:

```text
users/{uid}
```

Campos esperados:

```json
{
  "uid": "firebase-auth-uid",
  "name": "Nome do usuário",
  "email": "usuario@empresa.com",
  "phone": "",
  "role": "empresa_admin",
  "companyId": "org_123",
  "status": "active",
  "createdAt": "serverTimestamp",
  "updatedAt": "serverTimestamp",
  "lastLoginAt": "serverTimestamp",
  "preferences": {}
}
```

Papéis aceitos no frontend:

- `admin_valora`
- `consultor_valora`
- `empresa_admin`
- `gestor_pesquisa`
- `participante`

Se `users/{uid}` não existir, o acesso é bloqueado com a mensagem: “Seu usuário ainda não possui perfil configurado. Solicite liberação ao administrador.” Se `status` for diferente de `active`, o acesso também é bloqueado.

## Custom claims

Defina claims pelo Admin SDK/Cloud Functions, por exemplo:

```js
await getAuth().setCustomUserClaims(uid, {
  role: 'empresa_admin',
  companyId: 'org_123'
});
```

O código lê claims com `getIdTokenResult(true)` e usa `role` da claim quando ela está presente e é válida. Ainda assim, regras e Functions devem validar `request.auth.token.role`, `companyId` e o documento `users/{uid}` em toda operação sensível.

## Segurança de sessão

- Em `STORAGE_MODE: 'firebase'`, senha e perfil sensível não são gravados no `localStorage` da aplicação.
- O logout chama `signOut()`, limpa o perfil em memória e remove caches locais do namespace Firebase/Valora.
- O modo local continua usando credenciais de demonstração e `localStorage` apenas para MVP/demo.
- Não use `role` do frontend como autorização real; ele serve somente para menus e UX.

## Implantação

```bash
firebase login
firebase use <seu-projeto>
firebase deploy --only firestore:rules,hosting
```

Antes do go-live, implemente as Cloud Functions para links públicos, envio de e-mail/WhatsApp, relatórios pesados e qualquer operação administrativa. Revise `firestore.rules` para garantir segregação por `companyId`, papel e status ativo.

## Cloud Functions para links públicos de pesquisa

A pasta `functions/` contém as Functions HTTPS callable que devem ser publicadas junto com Firestore/Hosting em produção:

- `validateSurveyLink`: valida `surveyId` + token puro no backend, compara `sha256(token)` com `surveys/{surveyId}.tokenHash`, verifica status, prazo, empresa ativa, plano, revogação e limites, e retorna apenas dados mínimos ao participante.
- `submitSurveyResponse`: valida novamente o link, valida respostas obrigatórias, recalcula pontuação e dimensões no backend, força `companyId` da resposta a partir da própria pesquisa, registra IP/userAgent e consentimentos, salva em `responses/{responseId}` e grava auditoria.
- `getPublicResult`: consulta resultado público somente com `responseId` + `resultToken`; o token de resultado é salvo como hash em `resultTokenHash`.
- `auditLog` interno: registra ações em `auditLogs` com `action`, `actorType`, `uid`, `companyId`, entidade, antes/depois, IP/userAgent e `createdAt`.
- Rate limit básico: as Functions usam documentos em `rateLimits/{action}:{ip}:{surveyId}` para limitar validação, envio e consulta de resultado. Em produção de alto tráfego, complemente com Firebase App Check, Cloud Armor e alertas de abuso.

### Modelo mínimo de dados esperado

`surveys/{surveyId}` deve conter, no mínimo:

```json
{
  "companyId": "org_123",
  "formId": "form_123",
  "title": "Pesquisa",
  "status": "active",
  "startsAt": "Timestamp opcional",
  "expiresAt": "Timestamp obrigatório",
  "tokenHash": "sha256-do-token-puro",
  "allowRepeat": false,
  "requireIdentification": true,
  "lgpdRequired": true,
  "revoked": false,
  "responseCount": 0,
  "maxResponses": 1000
}
```

Nunca grave o token puro em Firestore em produção. Gere o link com o token puro apenas uma vez, envie ao participante e salve somente `tokenHash` na pesquisa.

### Gerar hash de token

Use SHA-256 no backend/Admin SDK ao criar ou renovar links:

```js
const crypto = require('crypto');
const tokenHash = crypto.createHash('sha256').update(token, 'utf8').digest('hex');
```

A Function auxiliar `hashSurveyToken` existe apenas para apoio administrativo controlado. Não exponha esse fluxo em telas públicas.

### Deploy das Functions

```bash
cd functions
npm install
npm run lint
cd ..
firebase use <seu-projeto>
firebase deploy --only functions,firestore:rules,hosting
```

### Frontend em modo Firebase

Quando `STORAGE_MODE: 'firebase'`, o frontend não valida token público nem lê `surveys`, `forms` ou `responses` diretamente para a jornada pública. A tela de participação chama `validateSurveyLink`, o envio chama `submitSurveyResponse` e o resultado público chama `getPublicResult`.

O `index.html` carrega o SDK compat de Cloud Functions e `firebase-init.js` expõe `ValoraFirebaseServices.functions` para essas chamadas.

### Regras e hardening recomendados

- Bloqueie leitura pública direta de `surveys`, `forms` e `responses` nas Firestore Rules; links públicos devem passar pelas Functions.
- Ative Firebase App Check para Hosting/Functions.
- Configure índices para consultas por `companyId`, `createdAt`, `surveyId` e `participant.email` usadas pelas Functions.
- Defina políticas de retenção para `auditLogs`, `rateLimits` e dados pessoais conforme a base legal aplicável e a política LGPD da controladora.

### Cloud Functions para e-mail, CEP e CNPJ em produção

Em `STORAGE_MODE: 'firebase'`, o frontend chama Cloud Functions (`sendEmail`, `getEmailStatus`, `lookupCep` e `lookupCnpj`) e não depende do `server.py`. O `server.py` permanece somente para demonstração local (`STORAGE_MODE: 'local'`).

Configure segredos e variáveis de ambiente antes do deploy:

```bash
firebase functions:secrets:set SMTP_PASSWORD
firebase functions:config:set smtp.host="smtp.seudominio.com.br" # legado, se utilizado no seu projeto
```

Para a implementação atual em Functions v2, defina também as variáveis de ambiente não secretas no ambiente de deploy/console Firebase:

```bash
SMTP_HOST=smtp.seudominio.com.br
SMTP_PORT=587
SMTP_SECURITY=starttls
SMTP_USERNAME=usuario@seudominio.com.br
SMTP_SENDER_EMAIL=nao-responda@seudominio.com.br
SMTP_SENDER_NAME="Valora Group"
```

Regras de segurança implementadas nas Functions:

- `sendEmail` exige usuário autenticado e limita envio a `admin_valora`, `empresa_admin` e `gestor_pesquisa`.
- `empresa_admin` e `gestor_pesquisa` só enviam templates `invite` e `result` vinculados à própria empresa.
- `participante` não tem envio livre.
- `SMTP_PASSWORD` é lido via Secret Manager e nunca enviado ao navegador ou gravado no Firestore.
- `getEmailStatus` retorna somente `configured`, `senderName` e `senderEmail` mascarado.
- `lookupCep` valida 8 dígitos, consulta ViaCEP com fallback BrasilAPI e aplica rate limit.
- `lookupCnpj` valida 14 dígitos, exige autenticação, consulta BrasilAPI e retorna apenas dados cadastrais necessários.

## Headers de produção e CSP

A versão canônica desta entrega é **8.6.6** (`APP_VERSION` em `config.js`). O Firebase Hosting usa `firebase.json` para enviar HTML com `Cache-Control: no-store` e assets JS/CSS/imagens com cache longo, desde que os assets continuem versionados no `index.html` com `?v=8.6.6`.

A política CSP inicial permite o funcionamento atual sem abrir permissões amplas: scripts da própria origem e SDK Firebase em `https://www.gstatic.com`, estilos locais com `'unsafe-inline'` temporário, imagens `self`, `data:` e `blob:`, conexões para `https://www.gstatic.com`, Firebase/Google APIs, Cloud Functions, ViaCEP e BrasilAPI, `object-src 'none'`, `base-uri 'self'` e `frame-ancestors 'self'`.

TODO de hardening: remover dependências de estilo inline para eliminar `'unsafe-inline'` de `style-src` em uma versão futura.

## Perfis e coleção `users/{uid}`

Para produção, cada usuário deve ter documento em `users/{uid}` com `role`, `companyId`, `department`, `position`, `status`, `receivesEmail` e `portalAccess`. O frontend já prepara os perfis oficiais e as regras Firestore foram ajustadas para permitir que `empresa_admin` crie apenas perfis de empresa (`empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `gestor_area`, `participante`, `convidado_externo`).

A criação real de usuários deve ocorrer por Firebase Auth/Cloud Functions ou convite seguro. O modo local/demo mantém senha inicial no armazenamento local apenas para demonstração.

## Repositório Firebase 2026
`firebase-repository.js` expõe métodos reais de Firestore para `organizations`, `users`, `plans`, `modules`, `forms`, `surveys`, `responses` e `invitations`. O modo local/demo continua em `local-repository.js` e mantém `companies` como compatibilidade para a interface atual.

## Repository Firestore real

`firebase-repository.js` carrega e salva dados reais nas coleções `organizations`, `users`, `plans`, `modules`, `forms`, `surveys`, `responses`, `invitations`, `invoices`, `settings` e `logs`.

### Compatibilidade com o app atual

- Firestore usa `organizations` como coleção canônica.
- O `app.js` segue lendo `state.companies`; o repository converte `organizations` para esse array.
- `logs` são somente leitura no cliente. Escrita de auditoria direta deve ser feita por Cloud Function/Admin SDK porque as rules bloqueiam gravação client-side.

### Seed mínimo

Use `firestore.seed.sample.json` como referência de estrutura. Substitua os IDs `AUTH_UID_*` por UIDs reais criados no Firebase Authentication. Campos `SERVER_TIMESTAMP` representam `FieldValue.serverTimestamp()` quando importados por script/Admin SDK.

Fluxo mínimo recomendado:

1. Criar o primeiro usuário no Firebase Auth.
2. Criar `users/{uid}` com `role: "admin_valora"`, `status: "active"` e `companyId: ""`.
3. Definir custom claims equivalentes com Admin SDK.
4. Criar `settings/public`, planos `free` e `essential`, módulos padrão e uma organização exemplo.
5. Criar um usuário Auth para `empresa_admin`, criar `users/{uid}` com `companyId` da organização e definir custom claims.
6. Publicar `firestore.rules` e testar isolamento por empresa antes de cadastrar dados reais.

### Carregamento por perfil

- `admin_valora`: carrega organizações, usuários, planos, módulos, formulários, pesquisas, respostas, convites, faturas, settings e logs somente leitura.
- `consultor_valora`: carrega dados operacionais globais permitidos pelas rules, sem depender de financeiro crítico.
- `empresa_admin`: carrega somente a própria organização e dados com o mesmo `companyId`, incluindo faturas quando permitido.
- `gestor_pesquisa`: carrega formulários, pesquisas, usuários necessários, respostas e convites da própria empresa.
- `analista_resultados`: carrega pesquisas e respostas da própria empresa, sem métodos de criação liberados pelas rules.
- `gestor_area`: carrega dados da própria empresa e filtra respostas por `department` quando o perfil possui área.
- `participante`: carrega perfil próprio e respostas próprias; portal público deve continuar via Cloud Functions.
- `convidado_externo`: não deve receber portal completo; use link público/Cloud Function.

### Erros tratados

O repository converte erros comuns em mensagens amigáveis: perfil sem permissão, sessão expirada, usuário sem perfil, empresa inativa, indisponibilidade de rede/Firestore e coleção vazia. Stack traces ficam restritos ao console técnico.

## Seed mínimo e primeiro acesso homologado

Use `firestore.seed.sample.json` como contrato de dados inicial para Firestore. O arquivo cobre `settings/public`, planos Gratuito/Essencial/Growth/Enterprise, módulos comerciais, organização exemplo, usuários sem senha e formulário global com escala, escolha única, múltipla escolha, texto curto/longo, questão correta, dimensões e faixas de resultado.

### Como rodar o seed

1. Crie os usuários no Firebase Authentication primeiro. Não grave senhas no seed.
2. Copie os UIDs reais para `users/AUTH_UID_ADMIN_VALORA` e, se desejar, `users/AUTH_UID_EMPRESA_ADMIN`.
3. Importe com script Admin SDK próprio, convertendo todo valor `SERVER_TIMESTAMP` para `FieldValue.serverTimestamp()`.
4. Publique custom claims coerentes, por exemplo `role: 'admin_valora'` e `companyId: ''` para o primeiro administrador.
5. Publique `firestore.rules` e Functions antes de liberar usuários finais.

### Jornada Firebase real validada pela arquitetura

- Primeiro admin: Firebase Auth + `users/{uid}` com `role=admin_valora`, `status=active` e claims equivalentes.
- Empresa: gravada em `organizations/{companyId}`; o frontend mapeia `organizations` para `state.companies` para preservar modo local/demo.
- Convites: em modo Firebase, envio em lote passa pela callable `sendSurveyInvitations`, que valida perfil, empresa da pesquisa, empresa ativa, plano, e-mails, participantes inativos e `receivesEmail=false`; cada convite é persistido em `invitations` com status padronizado.
- Resposta pública: `validateSurveyLink`, `submitSurveyResponse` e `getPublicResult` continuam sendo a barreira obrigatória. O `companyId` público é ignorado; a empresa vem da pesquisa. A resposta atualiza o contador da pesquisa e marca convite como `answered` quando houver `invitationId` ou e-mail correspondente.
- Gestor de área: a estrutura usa `department` em usuários, convites e respostas. O filtro de Firestore Rules aplica `department` quando o perfil é `gestor_area`; se um formulário ainda não coletar área explicitamente, a resposta fica filtrável quando `department` for preenchido no payload/convite.

### Status de convite aceitos

`pending`, `sent`, `opened`, `answered`, `expired`, `failed`, `resent`, `cancelled`.


## Secrets de observabilidade
```bash
firebase functions:secrets:set TELEGRAM_BOT_TOKEN
firebase functions:secrets:set TELEGRAM_CHAT_ID
```
O token do Telegram deve existir apenas em Cloud Functions/Secret Manager.

## Seed inicial obrigatório

Ativar Firebase não migra localStorage. Gere o export sanitizado no Admin Valora e importe com:

```bash
node scripts/import-firestore-seed.js --file ./exports/valora-local-export.json --project gestordepesquisa --dry-run
node scripts/import-firestore-seed.js --file ./exports/valora-local-export.json --project gestordepesquisa --apply --backup --create-auth-users --send-password-reset
```

A service account deve ficar fora do repositório e ser referenciada por `GOOGLE_APPLICATION_CREDENTIALS`.

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
