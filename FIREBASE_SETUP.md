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
