# Auditoria da Ponte Arquitetural Valora Pulse™

## Escopo analisado
Foram verificados `package.json`, `config.js`, `runtime-capabilities.js`, `index.html`, `app.js`, `firebase-init.js`, `firebase-repository.js`, `repository.js`, `communication-gateway/`, `functions/`, `scripts/` e `tests/` antes da aplicação da ponte.

## 1. Partes que ainda dependem de Firebase
- Inicialização/Auth/Firestore: `firebase-init.js`, `firebase-repository.js` e o provider atual `DATA_PROVIDER: 'firebase'` em `config.js`.
- Persistência operacional atual: empresas, usuários, planos, formulários, pesquisas, respostas, comunicações e auditoria ainda são carregados/salvos pelo repositório Firebase ou pelo estado local compatível.
- Gateway de comunicação usa Firebase Admin/Firestore para validar pesquisa pública e salvar resposta durante a fase de ponte.

## 2. Partes que ainda dependem de Cloud Functions
- `functions/` mantém callable/server functions legadas para compatibilidade.
- Pontos legados via `callPublicFunction`/`firebaseCallable` continuam existindo, mas a jornada pública só pode usá-los quando o provider for `firebase-functions` e `ENABLE_CLOUD_FUNCTIONS === true`.

## 3. Partes que já apontam para external-api
- `config.js` aponta `EXTERNAL_API_BASE_URL`, `COMMUNICATION_GATEWAY.baseUrl`, `PUBLIC_SURVEY_VALIDATION_PROVIDER`, `PUBLIC_SUBMISSION_PROVIDER` e `RESULT_PROVIDER` para `external-api` em produção.
- `gateway-client.js` centraliza chamadas JSON ao gateway/API.
- `api-client.js` e `api-repository.js` iniciam o provider API para login, cadastro, planos públicos e jornada pública.

## 4. Rotas públicas frágeis
- Validação de link público, submissão de resposta e consulta de resultado eram frágeis quando dependiam diretamente de Cloud Functions em Firebase Spark.
- O fallback local é útil para desenvolvimento, mas produção deve usar gateway/API para validação server-side, limite de plano, LGPD, auditoria e comunicação.

## 5. Onde a resposta da pesquisa é enviada?
- Frontend: `submitSurvey()` chama `submitPublicSurveyResponse()`.
- Provider `external-api`: `POST /public/surveys/:surveyId/responses` no Communication Gateway/API.
- Provider `local`: `submitSurveyResponseLocally()` salva em `state.responses`.

## 6. Onde o resultado é calculado?
- Frontend local: `calculateResult(form, answers)` em `app.js`.
- Gateway: `savePublicSurveyResponse()`/serviços de resultado calculam e persistem a devolutiva.
- API futura: projeto `backend/` prepara endpoint público para cálculo/persistência em PostgreSQL.

## 7. Onde o certificado é gerado?
- A geração/consulta atual permanece no frontend/serviços existentes de certificado e em rotas futuras do backend; nesta sprint o schema `valora.certificates` prepara persistência PostgreSQL.

## 8. Onde o e-mail deveria ser enviado?
- Nunca no frontend com segredo SMTP.
- Agora via Communication Gateway em `/communication/result/send` ou durante `/public/surveys/:surveyId/responses`, registrando falha sem invalidar a resposta.
- Futuramente via API Backend criando jobs em `communication.email_jobs`.

## 9. Dados que precisam migrar para PostgreSQL
- Empresas/organizações, usuários, unidades, planos/limites/capacidades, assinaturas/uso, formulários/dimensões/perguntas/opções, pesquisas/links, respostas/respostas por pergunta/scores, certificados, comunicações/jobs e auditoria.

## 10. Ordem segura para migração
1. Criar schema PostgreSQL e subir API em paralelo.
2. Exportar Firestore e validar mapeamento sem escrita destrutiva.
3. Migrar planos/catálogos e leitura pública de planos.
4. Migrar organizações/usuários com senhas re-hash ou reset seguro.
5. Migrar formulários/pesquisas/links.
6. Rodar gravação dual/híbrida para respostas e comunicações.
7. Comparar Firestore x PostgreSQL por período.
8. Alternar rotas públicas para API.
9. Alternar admin autenticado gradualmente.
10. Desativar dependências Firebase somente após auditoria e rollback testado.

## Pontos de chamada registrados
- `callPublicFunction`: wrapper legado em `app.js`, usado apenas por provider `firebase-functions` com Cloud Functions habilitadas.
- `firebaseCallable`: integração Firebase legada em repositórios/funções.
- `submitSurveyResponse`: função pública legada e endpoint gateway/API novo.
- `validateSurveyLink`: função pública legada e endpoint gateway/API novo.
- `sendSurveyInvitations`: permanece ligado a convites/admin e deve migrar para jobs de comunicação.
- `getEmailStatus`: permanece ligado ao status de transporte e gateway.
- `logServerEvent`: permanece ligado a observabilidade/auditoria e deve migrar para `audit.audit_logs`.
