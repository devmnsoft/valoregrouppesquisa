# Valora Insight™

Este repositório contém duas implementações durante a migração controlada.

## 1. Legado JavaScript/Firebase

Localização: raiz do repositório.

Estado: operação atual preservada até o cutover. Os arquivos de Hosting, Cloud Functions, Firestore Rules, scripts e testes legados permanecem na raiz para manter compatibilidade operacional.

## 2. Nova plataforma ASP.NET Core 10

Localização: `backend/`.

Solution: `backend/Valora.sln`.

Banco: PostgreSQL, schema `valorapesquisa`, com fonte canônica em `backend/database/postgresql/script_completo.sql`.

## Regras

- Não criar terceiro projeto.
- Não implementar novas funcionalidades em projetos paralelos.
- O antigo `projeto .NET predecessor removido` foi removido após consolidação.
- O legado só será aposentado após homologação e cutover.
- Documentação, scripts, banco e validadores específicos do novo backend devem permanecer dentro de `backend/`.
