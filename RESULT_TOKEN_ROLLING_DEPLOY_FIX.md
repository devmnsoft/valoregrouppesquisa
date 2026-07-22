# Correção de rolling deploy dos tokens de devolutiva

## Problema

Durante uma publicação parcial, `adminCreateResultShareLink` passou a criar tokens no contrato novo em `responses/{responseId}/resultAccessTokens/{sha256(rawToken)}`, mas revisões antigas de `getPublicResult` ainda validavam apenas `responses.resultTokenHash`. O link enviado por WhatsApp carregava `?result=<responseId>&rt=<rawToken>`, porém a revisão antiga rejeitava o token recém-gerado com `invalid_result_token` porque o ponteiro legado não era atualizado.

## Correção aplicada

- `resultAccessTokens` permanece como fonte principal para tokens ativos do contrato 2.
- `createResultAccessToken` agora cria o documento de acesso e, no mesmo batch, atualiza `responses.resultTokenHash` apenas como ponteiro temporário de compatibilidade para revisões antigas.
- `getPublicResult` normaliza o token bruto, calcula o SHA-256, valida primeiro a subcoleção, usa `responses.resultTokenHash` apenas como fallback legado e migra o fallback válido para a subcoleção.
- O bundle público e o retorno de `adminCreateResultShareLink` declaram `tokenContractVersion: 2`.
- O formato de URL continua exatamente `?result=<responseId>&rt=<rawToken>`; o hash nunca é usado na URL.
- Diagnósticos em `public_result_access_errors` são sanitizados e contêm apenas prefixo do hash, status e metadados de contrato.

## Privacidade e segurança

O token bruto não é persistido em Firestore, logs, auditoria, mensagens de erro ou URL com hash. Logs e auditorias usam apenas `tokenHashPrefix` com no máximo 8 caracteres.

## Deploy obrigatório

Publicar as duas funções impactadas juntas:

```bash
firebase deploy --only "functions:adminCreateResultShareLink,functions:getPublicResult" --project gestordepesquisa
```

Depois publicar o conjunto completo relacionado aos tokens:

```bash
firebase deploy --only "functions:submitSurveyResponse,functions:getPublicResult,functions:adminCreateResultShareLink,functions:adminRegenerateResultLink,functions:requestNewResultLink,functions:sendResultEmail,functions:getParticipantResultsByPassword" --project gestordepesquisa
```
