# Sprint de Estabilização da Jornada Principal

Documento atualizado na Sprint de Estabilização da Jornada Principal.

- Pesquisa pública usa provider configurável e não depende de Cloud Functions no Firebase Spark.
- Produção usa gateway externo quando `PUBLIC_SUBMISSION_PROVIDER='external-api'`.
- Fallback Firestore client só é permitido quando explicitamente configurado.
- Home prioriza pesquisa destaque válida antes dos planos.
- Certificados PDF/PNG usam ViewModel único e bloqueiam dados inválidos.
- Menu mobile usa `toggleMenu(force)` e fecha em navegação/logout.
- Cadastro Firebase cria Auth, organização e `users/{uid}` sem salvar senha em texto puro.
- Comunicação pós-pesquisa registra status e não bloqueia resultado.

## Validação recomendada
Execute `npm run check`, `npm run build:prod` e `tools/windows/35-validar-jornada-principal.bat` em Windows/IIS.
