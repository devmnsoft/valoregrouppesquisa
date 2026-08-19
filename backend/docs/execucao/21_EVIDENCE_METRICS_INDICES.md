# Evidence, Metrics e Índices

`evidence_items` agora registra explicitamente score original, referência da origem, estado do mapeamento e elegibilidade inferencial. Os estados produzidos são `mapped` e `pending_mapping`; pendências não alimentam Metrics, Índices ou Inferências.

Metrics e Índices continuam calculados por média ponderada por peso, polaridade e confiança. Menos de três evidências mantém o resultado como insuficiente, sem criar uma conclusão artificial.
