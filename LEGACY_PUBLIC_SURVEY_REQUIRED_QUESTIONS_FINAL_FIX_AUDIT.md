# Auditoria final — required questions no formulário público legado

## Escopo auditado
Arquivos revisados: `app.js`, `firebase-repository.js`, `repository.js`, `functions/index.js`, `style.css`, `scripts/build-production.js` e validadores `scripts/validate-*.js`.

## Achados e correções

1. **Home mostra “Diagnóstico temporariamente indisponível”**: o fallback público da home ficava em `renderHome`, no card `data-home-featured-public-fallback`, quando `getFeaturedHomeSurveyUrl()` ainda não tinha URL válida. A tela agora usa texto de preparação/carregamento para visitante e não renderiza formulário público na home sem URL.
2. **Falha de `getFeaturedHomeSurvey`**: em `functions/index.js`, a callable seleciona candidatos de destaque; quando nenhum candidato é aceito, tenta reparar/criar `official_free_survey` com `ensureOfficialFreeSurveyFallback({ markFeatured: true, forceRepair: true })` antes de retornar `ok:false`.
3. **Reparo de `official_free_survey`**: o reparo fica em `ensureOfficialFreeSurveyFallback` e também em `repairOfficialFormDocument`, que recriam organização/empresa, formulário oficial e pesquisa oficial com token real e hash válido.
4. **Renderização do formulário**: `renderTakeSurvey` valida o link, chama `setPublicSurveyContext`, exige estado `ready`, `surveyId`, `token`, `survey`, `form`, perguntas reais e `survey.formId === form.id` antes de montar o `<form data-form="takeSurvey">`.
5. **Contexto validado salvo**: `setPublicSurveyContext` grava `window.ValoraPublicSurveyState.context`, `window.ValoraPublicSurveyContext` e `renderedQuestionIds`, sempre com formulário real validado.
6. **Onde o contexto era perdido**: o submit reconstruía contexto misturando `getPublicSurveyContext`, `data(form)` e cache, aceitando `cached.form || {}`. Isso permitia enviar com form vazio.
7. **Form vazio na validação**: `validatePublicSubmitPayload` usava fallback de cache/objeto vazio. Agora exige `context.form.questions` real e retorna `public_form_questions_missing` antes da Function.
8. **`required_question_missing` chegava na Function**: antes, sem perguntas locais, o front não detectava required e chamava `submitSurveyResponse`; a Function carregava o form real e devolvia 400. Agora `buildPublicSurveySubmitPayload` e `validatePublicSubmitPayload` bloqueiam localmente.
9. **Bloqueio de submit com form vazio**: `buildPublicSurveySubmitPayload` lança `public_form_questions_missing`; `submitSurvey` mostra erro inline com `keepForm:true` e não chama `submitPublicSurveyResponse`.
10. **Destaque da pergunta obrigatória faltante**: `validatePublicSubmitPayload` adiciona `question-card-error` em cada `[data-question-id]` obrigatório faltante e foca `q_<id>`.
11. **Home deixa de ficar indisponível se fallback puder ser reparado**: a callable tenta reparar/criar fallback oficial e retorna payload `official_free_survey_fallback` com `survey`, `form`, `company`, `token`, `org`, `url` e `consistency` quando válido.

## Garantias implementadas

- Estado público único: `idle/loading/ready/submitting/submitted/unavailable/error` por `window.ValoraPublicSurveyState`.
- Formulário público possui hidden inputs reais `surveyId`, `token` e `org`.
- Coleta de respostas percorre somente `formDefinition.questions` validado e detecta pergunta ausente no DOM.
- `required_question_missing` é normalizado com mensagem amigável e nunca vira erro genérico de provider.
- Erros locais mantêm o formulário e respostas digitadas por meio de `publicSubmitInlineError`.
- Debug de submit registra `questionsCount` e `renderedQuestionsCount` em `window.ValoraRuntimeDiagnostics.lastPublicSubmitDebug`.
