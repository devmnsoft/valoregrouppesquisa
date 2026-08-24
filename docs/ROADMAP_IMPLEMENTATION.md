# Roadmap de implementação

## Entregue na base atual

- Separação Domain/Application/Infrastructure/Api/Web/Tests e contratos de dependência.
- Tenant, sessão BFF, catálogo canônico de acesso e menu autorizado.
- Diagnóstico, formulário, survey, link/resposta pública, resultados, relatórios e certificados.
- Camada explícita de metodologia, scoring ponderado, Evidence Pack, pipeline e inferência estruturada.
- Dashboard, Heatmap/Radar, Benchmark seguro, Evolution, Journey, Action e One-on-One em primeira versão.
- Console Master, planos/uso, notificações, integrações, auditoria, jobs e governança.
- SQL defensivo para instalações limpas/parciais e testes de regressão para Dapper/SQL/rotas.
- Fonte única no Domain para doze índices e quatro faixas oficiais.

## Próximas entregas priorizadas

1. **P0 — homologação com PostgreSQL real**: executar o SQL duas vezes em banco limpo e numa cópia anonimizada; realizar smoke autenticado com Super Admin e fluxos de escrita.
2. **P0 — anexos oficiais**: incorporar e versionar os dois PDFs metodológicos quando disponibilizados; comparar conceitos, pesos, prompts e templates com o briefing implementado.
3. **P1 — cobertura metodológica**: completar mappings oficiais e explicações por índice; medir cobertura e bloquear publicação incompleta.
4. **P1 — entregáveis**: homologar PDF/Word/Excel/JSON, QR/certificado, fontes e paginação em Windows/IIS.
5. **P1 — segmentação**: consolidar heatmaps/evolution por período, unidade, área, equipe e liderança respeitando limiar de anonimato.
6. **P2 — benchmark externo**: criar dataset governado, consentimento, anonimização, amostra mínima e versionamento antes de liberar referência externa.
7. **P2 — workers dedicados**: extrair processamento hospedado para `Valora.Workers` quando volume/SLA justificar, sem alterar contratos de Application.

## Critérios de saída

Build/testes verdes, script idempotente validado em PostgreSQL suportado, zero rota de menu sem action/view, zero permissão desconhecida, materialização Dapper coberta, autorização tenant/plan testada e nenhuma análise sem evidências rastreáveis.
