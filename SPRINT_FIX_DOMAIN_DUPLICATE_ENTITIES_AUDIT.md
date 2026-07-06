# Sprint Fix Domain Duplicate Entities — Auditoria final

## 1. Resumo

A sprint removeu a causa dos erros `CS0101` no domínio oficial, padronizando entidades duplicadas para uma definição por nome em `Valora.Domain.Entities`. Os agregadores `MigrationDomainEntities.cs`, `OperationalEntities.cs` e `LegacyMigrationEntities.cs` deixaram de declarar tipos e passaram a documentar a movimentação para arquivos próprios.

## 2. Causa dos erros CS0101

Os erros eram causados por arquivos individuais de entidades coexistindo com agregadores que declaravam os mesmos nomes no mesmo namespace `Valora.Domain.Entities`.

## 3. Entidades duplicadas encontradas

Foram tratadas as duplicidades de `UserProfile`, `SurveyParticipant`, `SurveyInvite`, `RolePermission`, `PrivacyRequest`, `Permission`, `OrganizationSettings`, `OrganizationModule`, `OrganizationBranding`, `LgpdConsent`, `EmailTemplate` e `CertificateValidation`.

## 4. Arquivos removidos ou ajustados

- `MigrationDomainEntities.cs`: declarações removidas e entidades movidas para arquivos próprios.
- `OperationalEntities.cs`: declarações removidas e entidades operacionais movidas para arquivos próprios.
- `LegacyMigrationEntities.cs`: declarações removidas e entidades históricas de migração movidas para arquivos próprios.
- Arquivos próprios criados/ajustados para as entidades operacionais, de migração e entidades duplicadas.

## 5. Propriedades preservadas

As versões oficiais preservam a união segura das propriedades úteis das versões duplicadas. Campos ligados a SQL/repositories, como `Protocol`, `RequesterEmailHash`, `ParticipantEmailHash`, `ValidationCode`, `Status`, `BodyText`, `ResultJson`, `SourceType` e `SummaryJson`, foram preservados nos arquivos próprios.

## 6. Padrão final adotado

- Uma entidade principal por arquivo.
- Nome do arquivo igual ao nome da entidade.
- Namespace `Valora.Domain.Entities`.
- Agregadores sem declarações de tipos duplicados.
- Propriedades identadas em múltiplas linhas nos arquivos tocados.

## 7. Ajustes de identação

Os arquivos de domínio tocados foram reformatados manualmente em múltiplas linhas. O validador também alerta sobre linhas longas remanescentes em outros projetos do backend oficial para apoiar uma próxima passada com SDK/dotnet format.

## 8. `.editorconfig`

Foi criado `.editorconfig` na raiz com indentação de 4 espaços para C#, newline final, organização de usings e severidade de formatação como warning/suggestion.

## 9. Validador criado

Foi criado `tools/validate-backend-domain-entities.js` e adicionado o script `backend:domain-entities-validate` ao `package.json`.

## 10. Testes criados

Foi criado `backend/Valora.Tests/DomainEntityDuplicateTests.cs`, teste estático que varre entidades de domínio e falha se houver `class`, `record`, `struct` ou `enum` duplicado por namespace.

## 11. Resultado do `dotnet restore`

Não executado com sucesso neste container porque o comando `dotnet` não está instalado (`/bin/bash: dotnet: command not found`).

## 12. Resultado do `dotnet build`

Não executado com sucesso neste container porque o comando `dotnet` não está instalado.

## 13. Resultado do `dotnet test`

Não executado com sucesso neste container porque o comando `dotnet` não está instalado.

## 14. Resultado dos validadores

- `npm run backend:domain-entities-validate`: passou sem duplicidade; emitiu alertas informativos de linhas longas/classes de uma linha ainda existentes em áreas não tocadas.
- `npm run backend:sql-schema-validate`: passou.
- `npm run backend:official-validate`: passou.
- `npm run backend:reports-email-validate`: passou.
- `npm run backend:migration-import-validate`: passou após manter comentários de contrato nos agregadores históricos.
- `npm run backend:homologation-cutover-validate`: passou.
- `npm run check:critical`: passou.

## 15. Comandos não executados e motivo

- `dotnet format backend/Valora.sln`: não executado porque o SDK .NET não está instalado.
- `dotnet format backend/Valora.sln --verify-no-changes`: não executado porque o SDK .NET não está instalado.
- `dotnet restore/build/test`: não executados pelo mesmo motivo.

## 16. Gaps restantes

O backend oficial ainda possui arquivos fora do domínio com linhas longas e algumas classes/records em uma única linha. O validador alerta esses pontos, mas não falha por eles para não bloquear a sprint em ambiente sem `dotnet format`.

## 17. Próximo passo recomendado

Executar a homologação real com PostgreSQL, validar SQL completo, API, Web, fluxos públicos, fluxos administrativos, importação, backup/restore e preparar `0.9.0-rc2` em ambiente com SDK .NET instalado.
