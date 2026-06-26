# CERTIFICADOS_VALORA_PREMIUM

Implementação 2026-06-23 para certificados premium, comunicação pós-pesquisa e aderência comercial dos planos.

- Certificados usam `buildCertificateViewModel` como fonte única, PDF em `ValoraPDF.createCertificate` e PNG por canvas próprio com `toBlob`.
- Comunicação usa `dispatchSurveyCompletedCommunications`, registra `communications` e não finge envio quando gateway/runtime não está ativo.
- Gateway esperado: `POST /communication/email/send`, `POST /communication/whatsapp/send`, `GET /communication/status`, com token no backend e sem SMTP no frontend.
- Planos oficiais: Grátis, Essencial, Profissional, Corporativo e Enterprise com `CAPABILITY_CATALOG` e `officialPlanCatalog`.

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
