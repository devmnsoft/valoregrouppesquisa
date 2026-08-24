# Catálogo de permissões

## Fonte de verdade

`Valora.Application/Access/ValoraAccessCatalog.cs` é a fonte canônica de códigos, capabilities e módulos de plataforma. `NavigationCatalog` referencia somente permissões/capabilities reconhecidas; o SQL semeia o mesmo conjunto. Código desconhecido é rejeitado no caminho estrito e reportado no caminho de compatibilidade.

## Perfis

- `admin_valora`: administração da plataforma; enxerga todos os módulos de plataforma independentemente do plano de um tenant.
- `consultor_valora`: operação autorizada em organizações vinculadas.
- `empresa_admin`: administração do tenant.
- `gestor_pesquisa`: configuração e coleta de diagnósticos.
- `analista_resultados`: leitura de resultados e inteligência permitida.
- `gestor_area`: leitura no escopo organizacional autorizado.
- respondentes: portal público/convite, sem acesso ao console.

## Regras

1. Autorização é a interseção de identidade ativa, vínculo com organização, papel/permissão, escopo e capability do plano.
2. Capability controla disponibilidade comercial; permissão controla ação. Uma não concede automaticamente a outra.
3. `units.read` é o código canônico para consulta de unidades; aliases históricos são migrados no SQL, não propagados para novas chamadas.
4. Itens do menu sem autorização não são renderizados; endpoints continuam exigindo autorização server-side.
5. Alterações de papel, permissão, exportação e configuração geram auditoria com usuário, tenant e correlação.

## Governança

Toda nova permissão deve ser adicionada ao catálogo, ao seed idempotente, à matriz de papel/plano, à política do endpoint e aos testes de navegação. Não é permitido introduzir string isolada apenas em view ou controller.
