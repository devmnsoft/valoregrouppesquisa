# Continuidade — Jornada de entrega executiva

Atualizado em 16/09/2026. Registro do incremento de preparação → revisão → publicação → download → compartilhamento autorizado → histórico de acesso.

## Base técnica

- SDK .NET 10 (`dotnet` 10.0.400 neste ambiente).
- Persistência canônica: `valorapesquisa.formal_deliverables` + `formal_documents` + `secure_share_links`.
- Geração PDF/XLSX/JSON: `ExecutiveReportExportService` (FormalDeliverables).
- Não foi criada uma segunda central de documentos; `generated_reports` (HTML/CSV legado) permanece para compatibilidade de API `/reports/*`.

## Mapa funcionalidade → implementação → lacuna → alteração → validação

| Funcionalidade | Existente | Lacuna | Alteração | Validação |
|---|---|---|---|---|
| Central de entregas | Lista client-side de `generated_reports` | Sem busca/filtros/paginação/estados editoriais | `IExecutiveDeliveryService.List` + UI `/Reports` | Unitários de serviço; build |
| Preparar relatório | Formulário survey+format | Sem resultado/template/seções/revisor | `PrepareAsync` + formulário tipado | Validação de título; hash de origem |
| Revisão/publicação | Ausente | Sem ciclo editorial | `SubmitForReview` / `Publish` com confirmação | Divergência de fingerprint bloqueia publicação |
| Geração/download | Formal PDF não exposto na UI | Bytes só em `formal_documents` | Publicação gera e grava; download só se `available` | Download bloqueado se não disponível |
| Compartilhamento | API share por diagnóstico | Sem vínculo a versão publicada | `CreateForDeliverableAsync` + revoke | Revogação impede resolve |
| Página pública HTML | Só JSON na API | Sem experiência Valora pública | Rotas `/entregas/publica/{slug}` (Web) | Manual / browser quando runtime disponível |
| Certificados | Emissão operacional sem critérios de maturidade | Risco de “certificação” inventada | `CertificateEligibility` com `maturity_criteria_undefined` | Teste unitário de pendência |
| Auditoria | `audit_logs` / export audit | Preparação/publicação pouco cobertas | Eventos `deliverable.*` | Cobertura em serviço |

## Distinções preservadas

- **Resultado**: apuração (`results` + scores).
- **Relatório**: apresentação versionada (`formal_deliverables` + arquivo em `formal_documents`).
- **Certificado**: emissão condicionada a tipo/critérios do template.
- **Compartilhamento**: autorização limitada por token (hash persistido; token bruto só na criação).

## Comandos

```bash
cd backend
dotnet build Valora.sln
dotnet test --filter "FullyQualifiedName~FormalDeliverables|FullyQualifiedName~ExecutiveDelivery"
```

Migration aditiva:
- consolidada no final de `database/postgresql/script_completo.sql`;
- também disponível em `database/postgresql/2026_09_16_executive_delivery_lifecycle.sql` para aplicação incremental.

## Superfície entregue

- Web `/Reports` — Central de Entregas (filtros, paginação, preparar, revisar, publicar, baixar, compartilhar, revogar).
- Web `/entregas/publica/{slug}` e `/p/e/{slug}` — página pública HTML (estados inválido/expirado/revogado/indisponível/falha).
- API `/api/deliverables*` — listagem, preparação, revisão, publicação, download, `secure-share`, elegibilidade de certificado.
- API `/share/{slug}` — JSON público; download usa bytes de `formal_documents` quando o link está vinculado a entregável.
- Políticas de permissão registradas no Web (`AddValoraPermissionHandler` + policies de `ValoraPermissions.All`).

## Evidências neste ambiente

| Verificação | Resultado |
|---|---|
| `dotnet build Valora.sln` | **Executado** — 0 erros |
| `dotnet test …FormalDeliverables\|ExecutiveDelivery` | **Executado** — 12 aprovados |
| Migration SQL aditiva + seed condicional | **Inspecionada** no `script_completo.sql` |
| PostgreSQL live (`VALORA_TEST_POSTGRES_CONNECTION`) | **Não disponível** — inserts/listagens reais não executados |
| Navegador autenticado ponta a ponta | **Não executado** — sem runtime Web+API+DB neste ambiente |
| Viewports / impressão | CSS e markup preparados; **não capturados** visualmente |

## Próximo incremento

1. Aplicar migration/seed em PostgreSQL de homologação (duas organizações).
2. Smoke autenticado: preparar → revisar → publicar → baixar → compartilhar → revogar.
3. Validar estados públicos (válido/expirado/revogado) e download bloqueado.
4. Confirmar viewports 1366×768, 1280×720, 768×1024, 390×844, 360×800 e impressão.
