# Sprint Fix Domain Duplicate Entities — Diagnóstico inicial

## Entidades duplicadas encontradas

A varredura estática em `backend/Valora.Domain/Entities/**/*.cs` encontrou duplicidade no namespace `Valora.Domain.Entities` para:

| Entidade | Tipo | Arquivos onde aparece | Diferença entre versões | Versão mantida | Versão removida | Referências a atualizar | Riscos |
|---|---|---|---|---|---|---|---|
| `UserProfile` | `record` | `UserProfile.cs`, `MigrationDomainEntities.cs` | Arquivo próprio tinha `Id`, `DisplayName`, `Department`, `Phone`, timestamps. Agregador tinha `OrganizationId`, `RoleCode`, `DepartmentId` e herança de auditoria. | Arquivo próprio, mesclado com campos úteis das duas versões. | Declaração do agregador. | Nenhuma referência direta precisou mudar; nome e namespace preservados. | Baixo: propriedades foram preservadas por união. |
| `SurveyParticipant` | `record` | `SurveyParticipant.cs`, `MigrationDomainEntities.cs` | Arquivo próprio tinha dados diretos de participante (`Name`, `Email`) e `Status=active`. Agregador tinha `ParticipantId` e `Status=invited`. | Arquivo próprio, mesclado. | Declaração do agregador. | Nenhuma referência direta precisou mudar. | Baixo: `ParticipantId` ficou opcional para compatibilizar cenários sem cadastro prévio. |
| `SurveyInvite` | `record` | `SurveyInvite.cs`, `MigrationDomainEntities.cs` | Arquivo próprio tinha `Id`, timestamps. Agregador tinha `TokenHash`. | Arquivo próprio, mesclado. | Declaração do agregador. | Nenhuma referência direta precisou mudar. | Baixo: token preservado. |
| `RolePermission` | `record` | `RolePermission.cs`, `MigrationDomainEntities.cs` | Arquivo próprio tinha timestamps; agregador herdava auditoria e não declarava `CreatedAt`. | Arquivo próprio, com `Id` e timestamps. | Declaração do agregador. | Nenhuma referência direta precisou mudar. | Baixo. |
| `PrivacyRequest` | `class`/`record` | `OperationalEntities.cs`, `MigrationDomainEntities.cs` | Operacional refletia SQL LGPD (`RequesterEmailHash`, `Protocol`, `ResponseId`, `HandledBy`, etc.). Agregador tinha modelo mínimo (`Email`, `RequestType`, `Status`). | Novo arquivo próprio baseado na versão operacional e com `Email` preservado. | Declarações dos agregadores. | Nenhuma referência direta precisou mudar. | Médio: há diferença de `DateTimeOffset` versus `DateTime`; foi mantido padrão operacional/SQL. |
| `Permission` | `record` | `Permission.cs`, `MigrationDomainEntities.cs` | Arquivo próprio tinha `Id`, `Name`, `ModuleCode`; agregador tinha `Description`. | Arquivo próprio, mesclado. | Declaração do agregador. | Nenhuma referência direta precisou mudar. | Baixo. |
| `OrganizationSettings` | `record` | `OrganizationSettings.cs`, `MigrationDomainEntities.cs` | Arquivo próprio tinha `SettingsJson`; agregador tinha flags LGPD/resultados e `TimeZone`. | Arquivo próprio, mesclado. | Declaração do agregador. | Nenhuma referência direta precisou mudar. | Baixo. |
| `OrganizationModule` | `record` | `OrganizationModule.cs`, `MigrationDomainEntities.cs` | Arquivo próprio tinha `ModuleId`, `Source`; agregador tinha `ModuleCode`, `BlockReason`. | Arquivo próprio, mesclado. | Declaração do agregador. | Nenhuma referência direta precisou mudar. | Baixo: `ModuleId` ficou opcional para permitir código textual. |
| `OrganizationBranding` | `record` | `OrganizationBranding.cs`, `MigrationDomainEntities.cs` | Arquivo próprio tinha `Id`, timestamps, cores opcionais; agregador tinha defaults e `PublicSlug`. | Arquivo próprio, mesclado. | Declaração do agregador. | Nenhuma referência direta precisou mudar. | Baixo. |
| `LgpdConsent` | `class`/`record` | `OperationalEntities.cs`, `MigrationDomainEntities.cs` | Operacional refletia SQL (`ParticipantEmailHash`, `ConsentVersion`, `Accepted`, IP/UA). Agregador tinha `Email` e data simples. | Novo arquivo próprio baseado na versão operacional e com `Email` preservado. | Declarações dos agregadores. | Nenhuma referência direta precisou mudar. | Médio: operacional segue SQL com `DateTimeOffset`. |
| `EmailTemplate` | `class`/`record` | `OperationalEntities.cs`, `MigrationDomainEntities.cs` | Operacional tinha `Name`, `BodyText`, `Status`, timestamps e soft delete. Agregador tinha núcleo de template. | Novo arquivo próprio baseado na versão operacional. | Declarações dos agregadores. | Nenhuma referência direta precisou mudar. | Baixo. |
| `CertificateValidation` | `class`/`record` | `OperationalEntities.cs`, `MigrationDomainEntities.cs` | Operacional tinha `ValidationCode`, `Status`, IP/UA e `DateTimeOffset`. Agregador tinha `ValidationCodeHash`. | Novo arquivo próprio baseado na versão operacional e com hash preservado. | Declarações dos agregadores. | Nenhuma referência direta precisou mudar. | Médio: operacional segue SQL de validações. |

## Plano objetivo da correção

1. Preservar o legado da raiz e não tocar em `backend-v2`.
2. Remover declarações duplicadas de agregadores sem esconder o problema com `partial`.
3. Criar arquivos próprios para entidades operacionais e de migração ainda agrupadas.
4. Mesclar propriedades úteis das versões duplicadas no arquivo oficial por entidade.
5. Manter `Valora.Domain.Entities` como namespace oficial.
6. Criar validador Node.js para bloquear futuras duplicidades por nome e namespace.
7. Adicionar teste estático em `Valora.Tests` para o mesmo contrato.
8. Criar/ajustar `.editorconfig` e executar `dotnet format` quando houver SDK.
9. Rodar build, testes e validadores disponíveis, documentando limitações de ambiente.
