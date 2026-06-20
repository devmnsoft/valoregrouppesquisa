# Relatórios executivos Valora Pulse

## Objetivo

A camada de relatórios executivos transforma os mesmos indicadores usados nos dashboards em entregáveis consultivos para diretoria, RH, governança, controladoria, consultores Valora, empresas clientes e participantes.

## Camada central

O arquivo `report-service.js` concentra a montagem dos relatórios e consome `ValoraAnalytics` para evitar divergência entre dashboard, área de respostas e exportações.

Funções disponíveis:

- `buildGlobalExecutiveReport(state, filters)` — relatório executivo global para Admin Valora.
- `buildCompanyExecutiveReport(state, companyId, filters)` — relatório executivo de empresa cliente.
- `buildSurveyReport(state, surveyId, filters)` — relatório por pesquisa.
- `buildParticipantReport(state, responseId)` — relatório individual do participante.
- `buildDimensionReport(state, companyId, dimensionId, filters)` — relatório por dimensão.
- `buildOnboardingReport(state, companyId)` — relatório de implantação.
- `buildPlanUsageReport(state, companyId)` — relatório de uso do plano.
- `buildExecutiveSummary(metrics)` — sumário executivo automático.

## Tipos de relatório

### Executivo global — Admin Valora

Inclui clientes totais e ativos, clientes em implantação, clientes travados, MRR estimado, plano mais usado, respostas, taxa média de resposta, pesquisas ativas, clientes próximos do limite, clientes sem resposta, ranking de uso e alertas comerciais.

### Executivo da empresa

Inclui capa/título com marca Valora, empresa, período, resumo executivo, funcionários/respondentes, pesquisas ativas, convites, respostas, taxa, média geral, maturidade por dimensão, pontos fortes, pontos críticos, recomendações e plano de ação sugerido.

### Pesquisa

Inclui dados da pesquisa, formulário, status, convites, respostas, taxa, resultado médio, dimensões, distribuição, comentários qualitativos disponíveis e recomendações.

### Participante

Inclui participante, pesquisa respondida, data, resultado geral, nível/faixa, resultado por dimensão, recomendações individuais e plano de melhoria pessoal. O certificado permanece disponível no fluxo individual.

### Dimensão

Inclui dimensão analisada, nota média, percentual, evolução, fragilidades, forças e recomendações específicas.

### Implantação

Inclui empresa, status de onboarding, percentual de evolução, usuários, pesquisas, convites, primeira resposta e próximo passo sugerido.

### Uso do plano

Inclui plano contratado, limites e uso de pesquisas, respostas, funcionários/respondentes e módulos. Quando algum limite passa de 80%, o relatório sugere avaliação de upgrade.

## Formatos

A Central de relatórios exporta:

- PDF executivo nativo;
- Word compatível `.doc`;
- Excel compatível `.xls` com tabelas estruturadas;
- CSV para dados tabulares;
- JSON para backup, integração e API.

## Fórmulas e consistência

- Respostas concluídas, convites enviados, média, dimensões, distribuição, evolução e uso do plano vêm de `analytics-service.js`.
- Média geral usa `normalized5`.
- Percentual usa `normalized5 / 5 * 100`.
- Taxa de resposta usa respostas concluídas únicas sobre convites enviados únicos.
- Dimensões são consolidadas a partir de `byDimension` gravado no cálculo da resposta.

## Permissões e LGPD

- Admin Valora pode exportar relatório global.
- Consultor Valora usa escopos permitidos pelo portal administrativo.
- Empresa Admin, Gestor de Pesquisa e Analista de Resultados ficam limitados à própria empresa.
- Gestor de Área permanece limitado ao escopo de área já aplicado nas telas de respostas.
- Participante vê somente relatório individual e certificado de suas próprias respostas.
- Relatórios nunca usam backup completo como fonte de exportação operacional.

## Estados sem dados

Os relatórios retornam mensagens amigáveis quando não há respostas, convites, dimensões ou plano identificado. A exportação ainda é gerada com seção de estado vazio, sem quebrar o modo local/demo.

## Auditoria

Cada exportação registra log com usuário, perfil, escopo, tipo de relatório, formato, filtros e data/hora pela função `audit` existente no frontend.

## Como testar

1. Login como Admin Valora e abrir **Relatórios**.
2. Exportar **Executivo global** em PDF, Excel, Word, CSV e JSON.
3. Login como Empresa Admin e abrir **Relatórios**.
4. Exportar **Executivo da empresa**, **Por pesquisa**, **Por dimensão**, **Implantação** e **Uso do plano**.
5. Comparar cards do dashboard com campos equivalentes do relatório.
6. Testar empresa sem respostas e confirmar mensagem amigável.
7. Testar participante e confirmar que apenas respostas próprias aparecem no relatório individual/certificado.

## Limitações atuais

- O PDF usa renderização nativa simples, sem biblioteca pesada; gráficos complexos são representados por tabelas, métricas e barras/tabelas textuais.
- Ranking de perguntas melhores/piores depende de respostas por pergunta estarem estruturadas de forma padronizada nos dados históricos.
- Numeração avançada de páginas permanece limitada ao gerador PDF atual.
