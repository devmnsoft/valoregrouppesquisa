# Pesquisa destaque da Home — Valora Insight™

## Como o sistema escolhe
A Home chama `resolveHomeFeaturedSurvey(state)`. A prioridade é: `settings.featuredSurveyId` ativo com acesso público; depois pesquisas marcadas com `isFeatured`, `featuredOnHome` ou `homeFeatured`; depois pesquisa grátis/Valora Insight (`isFree`, `planId=free` ou título contendo Valora Insight).

## Campos obrigatórios
A pesquisa precisa estar `active`, `published` ou `open`, não expirada, `visibleOnHome !== false` e possuir `publicUrl`, `publicLink`, `token`, `publicToken` ou `accessToken`. `tokenHash` sozinho não é token público e não deve abrir a Home.

## Link público
`buildHomeFeaturedSurveyUrl(survey)` gera `index.html?survey=[surveyId]&token=[token]&org=[slug]`, reaproveitando `publicUrl/publicLink` quando já existem. `ensureSurveyPublicLink(survey)` cria token seguro e persiste `publicUrl/publicLink` nos fluxos de criação/edição/renovação.

## Definir pelo Admin
Acesse Admin > Pesquisas e use **Definir como destaque da Home**. Para remover, use **Remover destaque da Home**. Admin > Status do Ambiente exibe ID, título, status, expiração, flags, token, publicUrl, link final e motivo quando não aparece.

## Reparar Firestore
Execute dry-run e backup antes de aplicar:

```bash
node scripts/repair-featured-home-survey.js --project gestordepesquisa --backup --dry-run
node scripts/repair-featured-home-survey.js --project gestordepesquisa --backup --apply
node scripts/validate-featured-home-survey.js --project gestordepesquisa
```

No Windows, use `tools/windows/31-reparar-pesquisa-destaque-home.bat`.

## Testar no navegador
Abra a Home e confirme a seção **Diagnóstico gratuito**, o botão **Responder diagnóstico grátis** e o `href` com `?survey=` e `token=`. Ao clicar, a rota pública deve chamar `renderTakeSurvey`.

---

## Atualização — Sprint Jornada Principal

- Pesquisa pública usa provider configurável e não depende de Cloud Functions no Firebase Spark.
- Produção usa gateway externo quando `PUBLIC_SUBMISSION_PROVIDER='external-api'`.
- Fallback Firestore client só é permitido quando explicitamente configurado.
- Home prioriza pesquisa destaque válida antes dos planos.
- Certificados PDF/PNG usam ViewModel único e bloqueiam dados inválidos.
- Menu mobile usa `toggleMenu(force)` e fecha em navegação/logout.
- Cadastro Firebase cria Auth, organização e `users/{uid}` sem salvar senha em texto puro.
- Comunicação pós-pesquisa registra status e não bloqueia resultado.
