# Auditoria final — resposta excluída, mobile e copy

1. A frase removida aparecia em `app.js`, na função `buildNaturalTransition`, dentro da devolutiva pública do Valora Insight™.
2. A função que monta “Próximo passo natural” é `buildNaturalTransition`, chamada por `buildValoraInsightDevolutiva`.
3. O relatório PDF usa a devolutiva montada por `buildValoraInsightDevolutiva`, então herdava o texto da transição.
4. O e-mail usa modelos de resultado e links da devolutiva; a busca não encontrou template separado com o texto comercial além da origem comum.
5. Templates de WhatsApp não possuíam o texto comercial como template separado; compartilhamentos usam links/CTA e contexto do resultado.
6. O bloco mobile “Diagnóstico gratuito Valora Insight™” é renderizado em `renderHome`, no `section.featured-survey-section`.
7. As classes que controlam fonte/layout do bloco agora são `.free-diagnostic-mobile-card`, `.free-diagnostic-copy`, `.free-diagnostic-benefits`, `.free-diagnostic-benefit` e `.free-diagnostic-start-card`.
8. O botão “Responder diagnóstico gratuito” é renderizado em `renderHome` como link `.btn.btn-primary[data-home-featured-cta]` dentro de `.free-diagnostic-start-card`.
9. A action de excluir resposta fica em `createActions`, mapeada para `deleteResponse`, `adminDeleteResponse` e `responseDelete`.
10. Ao clicar em “Excluir”, a função chamada é `adminDeleteResponse`.
11. Antes, o fluxo podia delegar para `deleteResponse` e cair em alteração local; agora exige `ValoraRepository.adminDeleteResponse`.
12. A exclusão chama repositório em `app.js` via `ValoraRepository.adminDeleteResponse(responseId)`.
13. O repositório persiste em Firestore via callable `adminDeleteResponse`, no localStorage via `local-repository.js`, e na API via endpoint `DELETE /responses/:id` quando disponível; se não houver suporte, retorna erro explícito.
14. Listagens usam `isDeletedResponse`/`activeResponsesOnly` para filtrar `deleted`, `deletedAt` e `status='deleted'`.
15. O cache que poderia recarregar excluídas é invalidado removendo `window.ValoraRuntimeCache.responses` e `window.ValoraRuntimeCache.adminResponses` após exclusão.
16. Correções aplicadas: copy premium sem pressão comercial, layout mobile dedicado, soft delete persistente, filtros de listagem/métricas, callable com audit log, rules ajustadas e validadores npm.
