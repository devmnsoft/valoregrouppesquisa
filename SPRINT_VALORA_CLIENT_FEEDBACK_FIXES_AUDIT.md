# Sprint Valora Client Feedback Fixes — Audit

## 1. Resumo
Correções aplicadas na Web oficial ASP.NET MVC/Razor, Application, API e SQL para nomenclatura Valora Insight™, mobile, datas, WhatsApp, certificado/relatório, menu público e validação automatizada.

## 2. Problemas relatados
Foram tratados estouro mobile, desalinhamento, repetição visual, ausência/erro de data, `Pulse`, `HOME`, contato incorreto, WhatsApp quebrado, espaço em branco de certificado, falha de baixar relatório e mensagens indevidas.

## 3. Correções de nomenclatura
Textos de produto/diagnóstico foram padronizados para `Valora Insight™`; nomes técnicos sem texto público, como `ValoraPulse` em configuração, foram preservados para não quebrar runtime.

## 4. Correções de data
Criado `formatValoraDate(value)` em JS público e aplicado no resultado com fallback `Data não informada`.

## 5. Correções de mobile
CSS público agora bloqueia overflow horizontal, limita cards/containers/imagens e empilha CTAs no mobile.

## 6. Correções de resultado/devolutiva
Resultado público foi reorganizado na ordem solicitada, com um único destaque principal e cards brancos apenas para conteúdos distintos.

## 7. Correções de certificado/relatório
Criado `valora-print.css` com `@media print`, margens A4, remoção de elementos flutuantes e prevenção de quebras/espaços excessivos.

## 8. Correções de WhatsApp
Links e rota `/whatsapp` usam `https://wa.me/5591992545353` com mensagem pré-preenchida, nova aba para links externos e `rel="noopener noreferrer"` nas views.

## 9. Correções de menu/topo
Menu público mantém `Início`; validador bloqueia `HOME`/`Home` visível em views públicas.

## 10. Explicação dos perfis
Home pública passou a explicar Administrador Valora, Empresa Cliente e Participante em linguagem simples.

## 11. Validador criado
Criado `tools/validate-valora-client-feedback-fixes.js` e script `npm run web:client-feedback-fixes`.

## 12. Checklist criado
Criado `VALORA_CLIENT_FEEDBACK_FIXES_CHECKLIST.md` para homologação em Android, iPhone, Chrome, Edge e jornadas públicas.

## 13. Comandos executados
- `npm run web:client-feedback-fixes` — PASS.
- `npm run web:brand-assets` — FAIL por assets oficiais ausentes no working tree.
- `VALORA_ALLOW_MISSING_BRAND_ASSETS=true npm run web:brand-assets` — PASS diagnóstico com aviso de assets ausentes.
- `npm run web:rc2-visual-readiness` — PASS.
- `npm run web:public-legacy-parity` — PASS.
- `npm run web:valora-insight-public-journey` — PASS.
- `npm run web:admin-menu-profile-access` — PASS.
- `npm run security:no-service-account-secrets` — PASS.
- `npm run backend:sql-schema-validate` — PASS.
- `npm run backend:domain-entities-validate` — PASS com avisos preexistentes de linhas/classes longas.
- `npm run backend:official-validate` — PASS.
- `npm run check:critical` — PASS.

## 14. Comandos não executados e motivo
- `dotnet restore backend/Valora.sln`, `dotnet build backend/Valora.sln` e `dotnet test backend/Valora.sln` não foram executados porque o SDK .NET não está disponível no ambiente (`dotnet SDK not available`).

## 15. Gaps restantes
Homologação visual em celular real e prints comparativos antes/depois ainda são recomendados.

## 16. Próximo passo recomendado
Rodar homologação com o cliente em celular real, gerar prints comparativos e fechar o pacote RC2.
