# Auditoria da Transição Arquitetural — Valora Pulse™

## Estado atual
O repositório opera como frontend estático HTML/CSS/JavaScript publicado em IIS/hosting estático, com Firebase habilitado para Auth/Firestore e um `communication-gateway` Node.js para comunicação transacional. A evolução para backend próprio já começou com `backend/Valora.sln`, projetos ASP.NET Core, scripts PostgreSQL e providers de API no frontend.

## Firebase
Usado por `firebase-init.js`, `firebase-repository.js`, `repository.js`, SDKs carregados em `index.html` e Cloud Functions em `functions/`. O modo oficial atual permanece `DATA_PROVIDER: 'firebase'` para não quebrar produção.

## localStorage
Usado como fallback/local state por `local-repository.js`, `repository.js` e fluxos de sessão/cache do frontend. Deve permanecer apenas para desenvolvimento, cache e contingência.

## Cloud Functions
`functions/index.js` mantém validações e integrações, mas produção Spark está com `ENABLE_CLOUD_FUNCTIONS=false`. A jornada pública agora deve rotear por `validatePublicSurveyLink`, `submitPublicSurveyResponse` e `loadPublicResult` antes de qualquer Cloud Function.

## external-api
`config.js` aponta `EXTERNAL_API_BASE_URL` e `API_BASE_URL` para `https://api.valoragroup.mnsoft.com.br`; `communication-gateway` já usa contrato HTTP; `api-client.js` e `api-repository.js` preparam consumo do backend.

## Pontos que quebravam no Firebase Spark
Cloud Functions diretas para validar link público, submeter resposta e carregar resultado; envio automático de e-mail sem gateway; segredos SMTP/WhatsApp no cliente; cálculo final de resultado no frontend.

## Módulos que devem ir para backend
Autenticação/JWT, planos e limites, pesquisas públicas, respostas, cálculo Valora Insight, certificados, e-mail transacional, WhatsApp, auditoria, relatórios e migração de dados.

## Dados para PostgreSQL
Organizações, usuários, unidades, planos, assinaturas, uso mensal, formulários, dimensões, questões, opções, pesquisas, links públicos, respostas, respostas por pergunta, resultados, certificados, jobs de comunicação e auditoria.

## Ordem recomendada
1. Manter Firebase em produção e corrigir jornada pública sem Cloud Functions diretas.
2. Subir PostgreSQL local e migrations idempotentes.
3. Validar health, planos e auth no backend.
4. Migrar pesquisa pública para API em modo híbrido.
5. Comparar resultados Firebase/PostgreSQL.
6. Migrar comunicação e certificados.
7. Virar `DATA_PROVIDER='api'` por empresa piloto.

## Riscos técnicos
Divergência entre cálculo frontend/backend; dados Firebase com formatos heterogêneos; ausência de `dotnet`/Docker em alguns ambientes; tokens públicos legados; limites de plano não retrocompatíveis.

## Riscos comerciais
Interrupção da jornada pública, envio duplicado de e-mails, inconsistência de certificados, cobrança incorreta por uso mensal e mudança abrupta para clientes ativos.
