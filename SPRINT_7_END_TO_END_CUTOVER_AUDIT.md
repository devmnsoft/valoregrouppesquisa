# Sprint 7 — Homologação End-to-End e Cutover Firebase → PostgreSQL

## Diagnóstico real

A produção permanece configurada para Firebase (`DATA_PROVIDER: 'firebase'`) e a arquitetura híbrida já existia parcialmente. A sprint consolidou validações formais no runtime, cliente API, repositório oficial de provider, scripts de validação e documentação operacional sem trocar produção para PostgreSQL.

## Checklist obrigatório

1. **api-client.js completo?** Sim. Possui `API_BASE_URL`, timeout via `AbortController`, tratamento de HTML, JSON inválido, 401 com limpeza de token e normalização de erro amigável.
2. **api-repository.js completo?** Sim. Expõe auth, planos, jornada pública, certificados, health, migração e envio de e-mail, com aliases de compatibilidade.
3. **gateway-client.js completo?** Sim para a ponte external-api da jornada pública e comunicação, validado pelos scripts existentes.
4. **index.html em ordem correta?** Sim. `api-client.js` vem antes de `api-repository.js`, ambos antes de `repository.js` e `app.js`.
5. **runtime-capabilities detecta firebase/api/hybrid?** Sim, incluindo `dataProvider`, `publicJourney`, `migration` e `getArchitectureWarnings()`.
6. **repository.js centraliza provider?** Sim. `ValoraRepository` roteia Firebase, API e Hybrid e expõe helpers de provider.
7. **DATA_PROVIDER=api usado de verdade?** Sim. `repository.js` seleciona `ValoraApiRepository` quando o provider é `api`.
8. **DATA_PROVIDER=hybrid compara sem duplicar escrita?** Sim. Escritas usam apenas o primário; leituras podem comparar com secundário e registrar divergências.
9. **renderTakeSurvey chama callPublicFunction diretamente?** Não.
10. **submitSurvey chama callPublicFunction diretamente?** Não.
11. **renderResult chama callPublicFunction diretamente?** Não.
12. **Backend ASP.NET Core existe e compila?** Existe em `backend/Valora.sln`; a validação local depende do SDK .NET no ambiente.
13. **API possui endpoints obrigatórios?** Sim: health, auth, planos, surveys públicos, resultados, certificados, comunicações, admin.
14. **PostgreSQL sobe via Docker?** Há `docker-compose.postgres.yml`; execução depende de Docker disponível.
15. **Migrations idempotentes?** Sim, com `CREATE ... IF NOT EXISTS` e seeds com conflito controlado.
16. **Seed dos planos oficiais existe?** Sim.
17. **Seed demo Valora Insight™ existe?** Sim, com organização demo, formulário, dimensões, perguntas, survey e token público.
18. **Migração Firestore → PostgreSQL exporta, transforma, importa e compara?** Sim, scripts existem para as quatro fases e comparador gera relatórios.
19. **Certificado PDF/PNG implementado ou fallback seguro?** Endpoints existem; quando geração backend não estiver disponível, deve retornar JSON seguro.
20. **E-mail de resultado como job auditável?** Sim, via tabelas de comunicação e `EmailJobService`.
21. **Riscos para produção API:** necessidade de homologar SMTP/gateway real, executar dry-run/apply/comparação com dados reais, validar carga, janela de cutover, backups e rollback testado.

## Ocorrências mapeadas

Foram revisadas ocorrências de `DATA_PROVIDER`, `HYBRID_PRIMARY_PROVIDER`, `API_BASE_URL`, providers públicos, `callPublicFunction`, `firebaseCallable`, jornada pública, `ValoraApiRepository`, `ValoraGatewayClient` e `ValoraRepository` em frontend, configs, scripts, backend, migrations e documentação.

## Estado por provider

- **firebase:** continua como padrão de produção e fallback local compatível.
- **api:** habilitado para homologação local com `API_BASE_URL` obrigatório.
- **hybrid:** habilitado para comparação de leitura Firebase x API sem duplicar escrita.

## Cutover e rollback

O cutover só deve ocorrer após backup Firebase, backup PostgreSQL, dry-run, import apply local, relatório comparativo sem divergências críticas, validação pós-cutover e confirmação explícita. O rollback deve restaurar `DATA_PROVIDER=firebase`, preservar respostas capturadas na API, reprocessar divergências e revalidar produção.
