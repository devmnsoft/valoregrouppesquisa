# Auditoria da Fase 02F

## Evidências

- `AuthenticationSessionService` gera material criptográfico e persiste apenas SHA-256 por meio dos repositórios.
- `RefreshTokenRepository.RotateAsync` bloqueia o token, consome uma única vez e revoga família e sessão em reutilização.
- `AuthController` expõe refresh, logout, logout global, listagem e revogação com contratos tipados.
- `DistributedBffSessionStore` usa `IDistributedCache` e Data Protection; o cookie contém somente ticket opaco.
- `PermissionRepository` resolve RBAC pelas tabelas `user_roles`, `role_permissions` e `permissions`.

## Limitações

Cadastro empresarial totalmente transacional, CNPJ externo, shell visual completo e a suíte Playwright multiviewport permanecem pendentes; não há declaração de aceite desses itens.
