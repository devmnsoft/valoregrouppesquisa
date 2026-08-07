# Valora Insight™ — V8 Implantação, Governança e Release

## Auditoria inicial

A auditoria foi feita sobre o novo sistema ASP.NET (`Valora.Api`, `Valora.Application`,
`Valora.Domain`, `Valora.Infrastructure` e `Valora.Web`), sem remover ou redirecionar o legado.

### Problemas encontrados

- O console Admin Valora concentrava CRM e cadastros enterprise, mas não possuía fluxos de
  implantação, checklist de produção, backup, qualidade, governança de acesso e release.
- A página Razor de pesquisa pública carregava um script que tratava somente resultados; o
  formulário de resposta, portanto, não era materializado nessa rota.
- Migração CSV cobria somente estrutura/membros e validava apenas nome e e-mail.
- Não existia política central e testável para mínimo de anonimização ou atualidade do backup.
- A identificação de ambiente/build não estava disponível no console operacional.
- As configurações genéricas aceitavam poucos tipos operacionais e não validavam os status
  específicos da implantação, checklist, LGPD ou backup.
- A persistência `enterprise_items` já existia e possui escopo de organização, exclusão lógica e
  índice por tipo/organização/status; ela foi reutilizada para evitar uma segunda fonte de verdade.

## Funcionalidades implementadas

- Implantação assistida persistente com empresa, responsáveis, plano, datas, observações, quinze
  etapas, progresso e conclusão individual.
- Checklist de Produção persistente com 21 verificações, responsável, observação, data de conclusão,
  status e link de ação.
- Console operacional para backup, LGPD, qualidade de dados, governança de permissões e de planos,
  usando registros reais e auditados; backup registra evidência/status, sem fingir executar o
  provedor externo.
- Endpoint autenticado de versão, data, ambiente e build; homologação recebe badge explícito.
- Release notes persistentes e publicáveis pelo Admin Valora.
- Migração ampliada para empresas, usuários, unidades, setores, pesquisas e respostas; a prévia
  bloqueia CNPJ inválido, resposta órfã, nome/e-mail inválidos e gera token SHA-256 da simulação.
- Política de anonimização com piso mínimo de três respostas, mínimo configurável e bloqueio de
  detalhe individual em pesquisa anônima.
- Jornada pública reparada com aceite obrigatório do termo v8.0 e escolha explícita entre resposta
  identificada e anônima.
- Auditoria das gravações operacionais por tipo (`enterprise.<kind>.saved`) no serviço de aplicação.

## Telas alteradas

- `Admin Valora`: menu reorganizado, badge de ambiente, Implantação, Checklist de Produção,
  Governança de Planos, Permissões, Qualidade dos Dados, Migração, Backups, LGPD e Release Notes.
- `Responder pesquisa`: carregamento real, perguntas recebidas da API, consentimento e anonimização.
- Layout administrativo: timeline, progresso, cards responsivos, ações persistentes e estados vazios.

## Segurança e governança

- Todos os endpoints do console permanecem autenticados; módulos globais são exclusivos do papel
  `admin_valora`, enquanto consultas organizacionais usam o `organization_id` do token.
- Atualizações usam a condição `organization_id IS NOT DISTINCT FROM`, impedindo editar item de
  outra organização por identificador conhecido.
- Segredos de backup são recusados pela aplicação; devem vir de variável/cofre do provedor.
- Conteúdo inserido na interface é escapado; falhas exibem mensagem humana e correlation ID continua
  propagado pelo BFF.
- O backend valida status por módulo e campos estruturais antes de persistir ou auditar.

## Validações executadas

- `node --check` nos scripts do console e da pesquisa pública.
- `python3 -m json.tool` nas configurações da API.
- `git diff --check`.
- O build/teste .NET foi solicitado, porém o container não possui o executável `dotnet`.

## Pendências reais

- Conectar `backup` ao provedor de infraestrutura escolhido (RDS/PostgreSQL gerenciado, cofre e
  armazenamento imutável). A V8 registra política, evidência, falha e teste de recuperação; não
  executa cópia insegura a partir da aplicação web.
- Aplicar a marca d'água “Homologação” nos geradores binários de PDF/certificado de cada provedor;
  o ambiente já é exposto de modo seguro para essa integração.
- A política de anonimização deve ser aplicada às consultas SQL agregadas específicas que forem
  adicionadas aos comparativos/exportações; a política e endpoint central já impedem decisões
  divergentes.
- Executar `dotnet restore`, `dotnet build` e `dotnet test` em agente com o SDK fixado por
  `backend/global.json`, além de smoke test com PostgreSQL inicializado.
- O histórico operacional usa `enterprise_items`; uma futura escala elevada pode justificar tabelas
  dedicadas para steps/checklist, preservando o mesmo contrato da aplicação.

## Operação local

1. Inicialize o PostgreSQL e aplique `backend/database/postgresql/script_completo.sql`.
2. Configure connection string/JWT via variáveis de ambiente; não grave segredo no JSON.
3. Execute `dotnet run --project backend/Valora.Api` e `dotnet run --project backend/Valora.Web`.
4. Entre como Admin Valora e acesse `/AdminValora?module=implementation`.
5. Para homologação, defina `Valora__Environment=homologation`, `Valora__BuildId=<hash>` e reinicie.
6. Valide uma pesquisa em `/public/surveys/{id}` com link/token emitido pela API.
