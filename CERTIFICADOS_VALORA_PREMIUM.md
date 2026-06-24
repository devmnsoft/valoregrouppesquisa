# CERTIFICADOS_VALORA_PREMIUM

Implementação 2026-06-23 para certificados premium, comunicação pós-pesquisa e aderência comercial dos planos.

- Certificados usam `buildCertificateViewModel` como fonte única, PDF em `ValoraPDF.createCertificate` e PNG por canvas próprio com `toBlob`.
- Comunicação usa `dispatchSurveyCompletedCommunications`, registra `communications` e não finge envio quando gateway/runtime não está ativo.
- Gateway esperado: `POST /communication/email/send`, `POST /communication/whatsapp/send`, `GET /communication/status`, com token no backend e sem SMTP no frontend.
- Planos oficiais: Grátis, Essencial, Profissional, Corporativo e Enterprise com `CAPABILITY_CATALOG` e `officialPlanCatalog`.
