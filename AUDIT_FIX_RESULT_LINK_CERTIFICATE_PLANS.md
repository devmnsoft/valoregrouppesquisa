# Auditoria — links de resultado, certificado e planos

## Causas
- O legado mantinha apenas `responses.resultTokenHash`; cada reenvio sobrescrevia o hash e invalidava links anteriores.
- O provider HTTP de e-mail era tratado como enviado em qualquer 2xx.
- O certificado estava desabilitado por flag/no-op no frontend legado.
- Planos tinham catálogos divergentes e partes do fluxo podiam gravar direto no Firestore.

## Solução aplicada
- Tokens públicos passam a coexistir em `responses/{responseId}/resultAccessTokens/{sha256(rawToken)}` com status, canal, autoria e auditoria sem token bruto.
- `getPublicResult` valida o hash do token recebido, faz fallback/migração de `resultTokenHash` legado e nunca retorna hashes.
- Reenvios criam tokens adicionais; falha de e-mail revoga apenas o token novo.
- Respostas HTTP de e-mail são normalizadas: `sent` exige confirmação e `messageId`; `202/pending` vira `queued`; rejeição vira `failed`.
- Certificado PDF básico foi reativado no resultado público e na administração usando dados já carregados/validados e `ValoraPDF.createCertificate`.
- O catálogo oficial de planos foi centralizado em `shared/plan-catalog.json` e consumido pelas Functions.
- Regras de Firestore bloqueiam gravação direta de coleções protegidas; o Admin SDK das Functions permanece como autoridade.

## Migração retrocompatível
Ao acessar uma resposta antiga com `responses.resultTokenHash`, `getPublicResult` valida o token bruto recebido e cria o documento em `resultAccessTokens` com canal `recovery`, sem persistir o token bruto.

## Testes e validação
Executar antes do deploy:
- `npm run check`
- `npm run test:rules`
- `npm run test:functions`
- `npm run test:security`
- `npm run build:prod`
- testes Playwright relevantes para resultado público/certificado.

## Deploy
1. Publicar regras: `firebase deploy --only firestore:rules`.
2. Publicar Functions: `firebase deploy --only functions`.
3. Publicar frontend estático.
4. Validar links antigos e novos em produção com tokens mascarados nos logs.
