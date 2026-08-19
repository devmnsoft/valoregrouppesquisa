# Pipeline de Inteligência

O fluxo produtivo existente foi mantido: extração de evidências, métricas ponderadas, índices, inferências e insights. Nesta execução, a elegibilidade deixou de depender apenas do JSON auxiliar: `mapping_status` e `can_be_used_for_inference` passam a ser campos canônicos da evidência.

O texto qualitativo sem score é preservado, mas não participa automaticamente de inferências. O reprocessamento continua usando a chave única resposta/pergunta/conceito e atualiza a evidência existente.
