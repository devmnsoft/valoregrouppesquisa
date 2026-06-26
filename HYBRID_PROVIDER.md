# Provider Híbrido

`DATA_PROVIDER='hybrid'` serve para diagnóstico controlado. A configuração `HYBRID_PRIMARY_PROVIDER` define se a submissão grava no Firebase ou na API para evitar duplicidade.

Regras:

- Validação pode comparar Firebase/gateway/API quando disponível.
- Submissão deve ocorrer apenas no provider primário.
- Divergências devem ser registradas para auditoria e QA.
- Produção deve permanecer em `DATA_PROVIDER: 'firebase'` até a virada planejada.
