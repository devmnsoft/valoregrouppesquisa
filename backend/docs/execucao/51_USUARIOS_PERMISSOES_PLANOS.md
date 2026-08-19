# Usuários, permissões e planos — execução 51

Foram preservados e reutilizados `IUserAdministrationService`, `IAccessAdministrationService`, `IPlanRepository` e `IPlanEntitlementService`. Os endpoints existentes de usuários, roles, permissões, plano atual, features, limites e uso continuam sendo a fonte canônica; não foi criada camada paralela. O script passa a manter históricos próprios para mudanças de permissão e eventos de uso consultáveis por período.

## Pendente real

A homologação integrada de convite, revogação de sessões, último administrador e bloqueios de limite exige PostgreSQL, configuração de autenticação e SDK .NET disponíveis.
