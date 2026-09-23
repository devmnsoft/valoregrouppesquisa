# Consolidação da jornada — auditoria de 2026-09

## Escopo verificado

O `HEAD` inicial era `d66686b806fd8cfef28376cb3c8aabcd66fc33cd`, na branch
`work`, sem alterações locais. Os anexos metodológicos de 52 e 109 páginas não
estavam no workspace e, portanto, não foram considerados como lidos ou
aprovados.

## Classificação dos achados

| Estado | Evidência no código |
| --- | --- |
| Corrigido | O índice de respostas referenciava `is_deleted` antes de a coluna convergir. A exclusão lógica já é o contrato consumido pelos repositórios; o bootstrap agora cria a coluna, preservando registros existentes, antes do índice. |
| Corrigido | A resposta era confirmada antes da criação do job. Resposta, auditoria, resultado e intenção de processamento agora são gravados na mesma transação; a chave lógica do job continua idempotente. |
| Corrigido | Score ausente podia cair na menor faixa do heatmap. Agora permanece insuficiente/indeterminado. |
| Corrigido | O cálculo isolado aceitava `lower_is_better`, mas o serviço de medição não carregava a regra do alvo. O alvo vigente mais recente passa a orientar a tendência. |
| Corrigido | O validador da URL pública dependia de distâncias arbitrárias em regex. A validação agora inspeciona corpos completos e a ordem dos guards. |
| Pronto com evidência estática | Revisão usa permissões distintas, antiforgery, versão esperada, chave de comando e rascunho segregado por organização, usuário e insight. Os testes passaram a validar Tag Helpers em vez de exigir URL literal. |
| Parcial | `insights` representa a saída determinística do pipeline; `valora_ai_insights` continua sendo a fila editorial sujeita a revisão humana. O provedor generativo permanece desabilitado e nenhuma saída foi fabricada para unir as filas. |
| Bloqueado no ambiente | Instalação/reaplicação PostgreSQL 15/16 e a jornada integrada não foram executadas porque `psql`, Docker e uma conexão descartável não estão disponíveis. |
| Bloqueado no ambiente | Restore, build, testes .NET e `dotnet format` não foram executados porque o SDK .NET 10 não está instalado. |
| Não verificado | Testes E2E com API/Web e PostgreSQL reais, assim como inspeção visual em 360/768/1440 px. |

## Fonte única de SQL e recuperação

`database/postgresql/script_completo.sql` permanece o único bootstrap e mecanismo
de convergência ativo. O recorte de entrega executiva foi movido para o histórico
documental, pois já está incorporado ao canônico. O cenário `workspace-77f0520`
é fixture sintética, opt-in e protegida para banco de teste; por isso reside nas
fixtures de testes e não no diretório do bootstrap.

O script canônico abre uma transação no início e usa transações adicionais por
bloco. Os wrappers usam `ON_ERROR_STOP`; assim, uma falha reverte o bloco aberto,
mas não deve ser descrita como atomicidade do arquivo inteiro. A recuperação é
corrigir a causa, reaplicar o canônico e conferir as pós-condições. A intenção de
processamento da coleta não precisa ser reconstruída após commit: ela participa
da mesma transação da resposta.

## Decisões metodológicas pendentes

- A faixa aplicável a exatamente seis evidências requer aprovação metodológica;
  nenhuma nova faixa, peso ou alegação de confiança foi criada neste ciclo.
- Registros históricos sem versão inequívoca devem continuar identificados como
  pendência. Não se deve vinculá-los automaticamente ao catálogo mais recente.
- Score, cobertura, confiança de evidência e inferência continuam conceitos
  distintos. Revisão humana e conclusão de ação não demonstram causalidade nem
  elevam maturidade automaticamente.
- A ativação futura de IA generativa exige schema integral de saída, sanitização,
  evidências autorizadas, limites, segredo seguro, timeout e revisão humana.

## Backlog de próximas sprints

1. Executar matriz limpa/parcial/legada, três reaplicações e pós-condições em
   PostgreSQL 15 e 16 descartáveis.
2. Substituir o antigo teste de homologação por uma jornada integrada que crie
   dados sintéticos, force retry e valide unicidade, isolamento e rollback.
3. Persistir snapshot imutável dos mapeamentos e versão metodológica por execução,
   com tratamento explícito dos registros legados ambíguos.
4. Integrar explicitamente a saída determinística elegível à fila editorial sem
   duplicação e mantendo o provedor generativo desabilitado.
5. Executar E2E e acessibilidade da jornada em API/Web/banco reais nas larguras
   360, 768 e 1440 px.
