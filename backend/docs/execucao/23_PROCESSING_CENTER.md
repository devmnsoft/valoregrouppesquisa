# Processing Center

Além das rotas detalhadas de jobs, `GET /api/v1/intelligence/processing` expõe a listagem paginada canônica. O acesso ao centro valida tanto `organizational_intelligence.generate` quanto o entitlement `organizational_intelligence`; bloqueios de plano retornam `PLAN_UPGRADE_REQUIRED` com `correlationId`, sem erro 500.

O workspace mantém suas rotas próprias e também oferece os contratos públicos por diagnóstico: `processing-status`, `process-intelligence` e `reprocess-intelligence`. O reprocessamento reutiliza a proteção contra job ativo, portanto não abre execução concorrente para o mesmo diagnóstico.

O Processing Center existente permanece apoiado pela fila idempotente, runs e etapas persistidas. Esta execução não criou rotas paralelas nem alterou a preservação das respostas em falhas.

Os estados e mensagens continuam sendo exibidos a partir dos jobs reais, com correlation ID e erro sanitizado. A validação completa depende de um ambiente com .NET e PostgreSQL disponíveis.
