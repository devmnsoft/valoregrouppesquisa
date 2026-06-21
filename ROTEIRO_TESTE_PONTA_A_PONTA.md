# Roteiro de Teste Ponta a Ponta — Valora Pulse

**Versão:** Valora Group™ 8.6.4 RC1
**Data:** 2026-06-21
**Objetivo:** validar a jornada principal do produto do ponto de vista comercial, operacional, participante, analítico e de segurança.

## Pré-condições

- Servidor local iniciado ou ambiente Firebase publicado.
- Base local recriada ou seed Firebase importado.
- Console do navegador aberto para captura de erros.
- Usuários de teste disponíveis para Admin Valora, Empresa Admin e Participante.
- Plano de teste definido com limites conhecidos.

## Fluxo principal

| Ordem | Ação | Resultado esperado | Status | Evidência |
|---:|---|---|---|---|
| 1 | Entrar como Admin Valora | Dashboard global abre sem erro crítico. | Não testado |  |
| 2 | Criar empresa | Empresa é gravada e aparece na lista. | Não testado |  |
| 3 | Definir plano | Plano aparece associado à empresa com limites corretos. | Não testado |  |
| 4 | Criar administrador da empresa | Usuário fica vinculado à empresa e perfil correto. | Não testado |  |
| 5 | Entrar como empresa admin | Portal mostra somente dados da própria empresa. | Não testado |  |
| 6 | Cadastrar funcionários | Funcionários aparecem e podem ser filtrados. | Não testado |  |
| 7 | Criar questionário | Questionário fica disponível para pesquisa. | Não testado |  |
| 8 | Criar pesquisa | Pesquisa ativa com vínculo ao questionário. | Não testado |  |
| 9 | Enviar convites | Convites são gerados/enfileirados/enviados conforme modo. | Não testado |  |
| 10 | Abrir link como participante | Link válido abre tela pública sem portal administrativo. | Não testado |  |
| 11 | Responder pesquisa | Resposta é salva após aceite LGPD e validações. | Não testado |  |
| 12 | Ver resultado | Participante vê resultado/certificado quando aplicável. | Não testado |  |
| 13 | Ver área de respostas | Empresa visualiza resposta da pesquisa. | Não testado |  |
| 14 | Ver dashboard | Indicadores refletem resposta, taxa e dimensões. | Não testado |  |
| 15 | Gerar relatório | Relatório abre/exporta com dados corretos. | Não testado |  |
| 16 | Criar plano de ação | Ação manual ou automática fica registrada. | Não testado |  |
| 17 | Gerar notificação | Notificação aparece para perfil correto. | Não testado |  |
| 18 | Gerar log | Log registra evento com mascaramento. | Não testado |  |
| 19 | Exportar relatório | PDF/Word/Excel/CSV/JSON funcionam conforme plano/perfil. | Não testado |  |
| 20 | Validar isolamento | Empresa/participante não acessam dados de terceiros. | Não testado |  |

## Critérios de parada

Interromper a homologação e abrir bug bloqueante se ocorrer:

- falha de login;
- falha de cadastro essencial;
- falha ao responder pesquisa;
- erro crítico no console em tela principal;
- cálculo incorreto;
- vazamento entre empresas;
- exposição de segredo/token;
- bypass de permissão.

## Evidências mínimas

- Print do dashboard Admin Valora.
- Print do cadastro da empresa.
- Print do portal da empresa isolado.
- Print do link público.
- Print da resposta concluída.
- Arquivo de relatório exportado.
- Print do log gerado.
- Print do console sem erro crítico.
- Registro dos bugs em `BUGS_HOMOLOGACAO.md`.
