# Guia de compatibilidade do `scriptbd_completo.sql`

Use `scriptbd_completo.sql` como bootstrap idempotente oficial do schema `valorapesquisa`. Ele contém uma seção `-- COMPATIBILIDADE PARA BANCOS EXISTENTES` que deve rodar antes de índices e seeds.

## Cenários suportados

- Banco limpo.
- Banco parcialmente criado.
- Banco antigo sem colunas novas.
- Segunda execução idempotente.
- Local/homologação e preparação para produção.

## Regras

- Não usar schema `public` para objetos oficiais.
- Não remover dados de negócio sem backup.
- Não reintroduzir `plans.price_label` ou `plans.badge`.
- Seeds oficiais devem usar apenas colunas garantidas pelo bloco de compatibilidade.

## Validação

```bash
npm run db:scriptbd-validate
npm run backend:sql-schema-validate
npm run backend:official-validate
npm run check:critical
```

Com PostgreSQL disponível, rode o script duas vezes e consulte as contagens oficiais de planos, limites, capacidades, formulários, perguntas, opções e super admin.
