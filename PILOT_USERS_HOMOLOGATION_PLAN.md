# Plano de Homologação com Usuários Piloto

Versão: 0.9.0-rc1
Data: 2026-07-02

## Perfis participantes e cenários
- `admin_valora`: criar organização, planos, módulos, usuários, consultar auditoria e health.
- `empresa_admin`: configurar organização, usuários, permissões e dashboard da empresa.
- `gestor_pesquisa`: criar formulário, perguntas, pesquisa, publicar link e acompanhar respostas.
- `analista_resultados`: consultar relatórios, exportações, certificados e dashboards.
- `participante`: acessar link público, responder pesquisa, ver resultado e receber e-mail em DevelopmentOutbox.
- `convidado_externo`: validar certificado público e consultar protocolo LGPD público.

## Critérios de aceite
- Fluxos concluídos sem erro técnico exposto.
- Nenhum segredo, hash, token ou e-mail completo exposto indevidamente.
- Auditoria registrada para ações críticas.
- Backup/restore testado em base descartável.

## Formulário de feedback
- Perfil:
- Cenário executado:
- Resultado esperado:
- Resultado observado:
- Evidência:
- Severidade: baixa/média/alta/bloqueante
- Sugestão:

## Bugs esperados e prioridade
- Bloqueante: falha de login, envio de resposta, cálculo de resultado, vazamento de segredo.
- Alta: relatório incorreto, certificado inválido, bloqueio de permissão incorreto.
- Média: mensagens pouco claras, lentidão moderada.
- Baixa: ajustes visuais e textos.

## Prazo e responsáveis
Prazo sugerido: 5 dias úteis após ambiente completo. Responsáveis: produto, QA, operação, desenvolvimento e suporte.

## Canal de suporte
Canal dedicado de homologação com triagem diária e registro em backlog.

## Critérios para liberar produção
Todos os bloqueantes e altas resolvidos, restore/build/test reais aprovados, backup/restore validado, pacote assinado/conferido e cutover manual aprovado.
