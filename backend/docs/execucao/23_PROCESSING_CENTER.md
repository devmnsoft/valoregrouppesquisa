# Processing Center

O Processing Center existente permanece apoiado pela fila idempotente, runs e etapas persistidas. Esta execução não criou rotas paralelas nem alterou a preservação das respostas em falhas.

Os estados e mensagens continuam sendo exibidos a partir dos jobs reais, com correlation ID e erro sanitizado. A validação completa depende de um ambiente com .NET e PostgreSQL disponíveis.
