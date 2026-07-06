# Auditoria final — correção de `rt` vazio no resultado público legado

## Arquivos auditados

- `app.js`
- `firebase-repository.js`
- `repository.js`
- `functions/index.js`
- `config.js`
- `config/config.production.js`
- `scripts/validate-*.js`

## Pontos auditados

1. `submitSurveyResponse` cria o `responseId` em `functions/index.js`, usando o id do documento `responses` antes de montar o payload salvo.
2. `submitSurveyResponse` cria `resultToken` com `createToken(32)` em `functions/index.js`, imediatamente após criar a referência da resposta.
3. `resultTokenHash` é calculado com `sha256(resultToken)` e salvo na response junto com `resultTokenCreatedAt` e `resultAccessEnabled`; o token puro não é persistido.
4. `resultToken` é retornado ao front somente na resposta da callable (`resultToken` e alias `accessToken`), junto com `ok: true`.
5. `handlePublicSubmitSuccess` monta a URL `?result=&rt=` em `app.js` apenas depois de validar `responseId` e `resultToken`; quando falta token, registra diagnóstico e mantém resultado imediato com aviso.
6. O link público do e-mail é montado por `publicResultUrl(response.id, resultToken)` em `functions/index.js`; a função agora rejeita token vazio antes de criar a URL.
7. `getPublicResult` em `functions/index.js` aceita `resultToken` ou `rt`, exige valor não vazio, compara `sha256(resultToken)` com `resultTokenHash` e não depende de `req.auth`.
8. A rota pública em `app.js` agora diferencia tentativa (`params.has('result')`) de rota completa (`responseId && resultToken`), renderizando erro amigável para `?result=...&rt=`.
9. A queda em `signInWithPassword` ocorria porque `isPublicResultRoute` só reconhecia resultado quando `result` e `rt` eram truthy; com `rt=` a URL deixava de ser tratada como rota pública completa e seguia para fluxo legado/autenticado.
10. Correções feitas: geração/retorno obrigatório de token, bloqueio de e-mail com link incompleto, URL pública com validação, `getPublicResult` estrito, rota pública incompleta sem Auth, diagnóstico de retorno da function, status honesto de e-mail, filtro de ruído externo e validadores de regressão.

## Observações de configuração

- `config.js` e `config/config.production.js` foram auditados para confirmar o contexto de Firebase/Hosting; não exigiram alteração para esta correção.
- `repository.js` já delega `loadPublicResult` para o provider público e não precisou de alteração estrutural.
