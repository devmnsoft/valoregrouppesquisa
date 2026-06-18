# Preparação para Firebase

A versão executável usa armazenamento local para demonstração. Para produção, migre cada domínio para serviços Firebase.

## Serviços recomendados

- Firebase Authentication: contas e sessões.
- Cloud Firestore: clientes, usuários, formulários, pesquisas, respostas, planos, faturas e configurações.
- Cloud Functions: links públicos, hashing/validação de token, e-mail, WhatsApp, CNPJ/CEP e relatórios pesados.
- Firebase Hosting: frontend.
- App Check: redução de abuso.
- Cloud Storage: certificados e arquivos, quando necessário.

## Coleções sugeridas

```text
organizations/{organizationId}
users/{uid}
forms/{formId}
surveys/{surveyId}
responses/{responseId}
plans/{planId}
invoices/{invoiceId}
settings/public
settings/private
logs/{logId}
```

Todos os documentos de negócio devem carregar `companyId`, exceto configurações e entidades globais.

## Links públicos

Não exponha a coleção `surveys` para leitura pública. Crie uma Function HTTPS que receba `surveyId + token`, compare o hash armazenado, valide status, prazo, limite, consentimento e retorne somente o conteúdo necessário. Respostas públicas também devem passar por Function para aplicar validação, rate limit e auditoria.

## Claims e papéis

Use custom claims para `admin_valora`, `consultor_valora`, `empresa_admin`, `gestor_pesquisa` e `participante`. Mantenha o `companyId` no documento do usuário e valide ambos nas regras e nas Functions.

## Segredos

SMTP, WhatsApp, APIs privadas e chaves de pagamento devem usar Secret Manager/configuração de Functions. Nunca coloque senhas no JavaScript, `firebaseConfig`, repositório ou Hosting.

## Implantação

```bash
firebase login
firebase use gestordepesquisa
firebase deploy --only firestore:rules,hosting
```

Antes de implantar, substitua a persistência local do `app.js` por um repositório Firestore e implemente as Functions de link e envio. O arquivo `firestore.rules` desta entrega é um ponto de partida conservador, não uma validação final da arquitetura.
