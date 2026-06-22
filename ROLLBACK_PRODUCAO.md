# Rollback de produção

## Identificar a versão atual

1. Verificar a release/tag implantada no GitHub.
2. Conferir o último run do workflow `Firebase production deploy`.
3. No Firebase Console, abrir Hosting e identificar a release ativa.
4. Registrar horário, autor, tag/commit e sintomas do incidente.

## Voltar para release anterior

Opção preferencial: usar o histórico do Firebase Hosting.

1. Acessar Firebase Console > Hosting > Histórico de releases.
2. Selecionar a release anterior conhecida como estável.
3. Acionar rollback para essa release.
4. Validar login, carregamento de assets, CSP e principais jornadas.

Opção por GitHub Actions:

1. Fazer checkout da tag/commit anterior.
2. Executar `npm run check`, `npm run security:check` e `npm run build:prod`.
3. Executar workflow manual `Firebase production deploy` com aprovação do environment `production`.

## Desativar canal problemático

Para preview/homologação, expirar o canal problemático:

```bash
firebase hosting:channel:delete pr-<numero> --project <projeto-homologacao>
```

## Restaurar configuração

- Reverter mudanças de `firebase.json`, Rules ou Functions apenas via PR/release aprovada, exceto incidente crítico.
- Rotacionar secrets se houver suspeita de exposição.
- Revalidar App Check, Rules, Functions e alertas após o rollback.

## Bug crítico pós-deploy

1. Abrir incidente e congelar deploys não relacionados.
2. Decidir entre rollback imediato e hotfix.
3. Rollback deve ser aprovado por responsável técnico e negócio.
4. Após estabilizar, criar post-mortem com causa raiz e ação preventiva.

## Aprovação

Rollback de produção deve ser aprovado por, no mínimo, responsável técnico/DevOps e responsável de negócio ou plantonista designado.
