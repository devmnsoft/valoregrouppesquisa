# Arquitetura de autenticação e sessões

## Contrato alvo
Access tokens devem ser JWT curtos com issuer/audience e chave exclusiva de ambiente. Refresh tokens devem ser aleatórios, persistidos apenas por hash, ligados a sessão e família, rotacionados em uso único e revogados em reutilização. Logout revoga a sessão; logout-all revoga todas as sessões do usuário.

## Recuperação
O token bruto existe apenas durante a montagem do link; banco e auditoria recebem somente hash. Respostas de recuperação são uniformes para impedir enumeração. Esta arquitetura é normativa; a implementação integral permanece pendente.
