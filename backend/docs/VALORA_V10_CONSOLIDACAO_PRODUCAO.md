# Valora Insight™ — auditoria e consolidação de produção V10

## Escopo e método

Auditoria estática realizada sobre domínio, script PostgreSQL canônico, repositories, services, controllers, BFF, Razor, JavaScript e testes. A classificação **completa** exige cadeia persistente e teste; a presença de uma tela isolada não foi considerada implementação.

## Funcionalidades completas encontradas

- Autenticação/sessão, organizações, usuários, permissões, formulários, pesquisas, respostas e cálculo de resultado possuem camadas de domínio, aplicação, infraestrutura, API/Web e testes.
- E-mail operacional possui configuração por ambiente, fila persistida, processor, histórico/status e tratamento explícito para ambiente sem SMTP.
- Relatórios, certificados, LGPD, importação/migração, estrutura organizacional e auditoria possuem persistência e serviços próprios. Certificados incluem consulta pública e escopo organizacional nos fluxos autenticados.
- API keys já armazenam somente hash/prefixo, escopos, revogação e último uso no modelo Enterprise.

## Funcionalidades parciais encontradas

- **Campanhas:** a tela V9 tratava pesquisas como campanhas. Criação, status e compartilhamento persistiam em `surveys`/`survey_links`, mas público-alvo, meta, unidade, setor e janela da campanha não tinham entidade própria. A V10 adiciona a estrutura persistente; endpoints e troca do frontend para essa estrutura ainda precisam ser concluídos.
- **Cockpit:** usa respostas reais, mas parte do índice de maturidade é uma aproximação de apresentação e não um agregado versionado.
- **Templates:** o uso cria formulário real; o catálogo oficial permanece versionado em código e não é editável no banco.
- **Ajuda:** artigos são estáticos e não possuem workflow editorial persistente.
- **Recomendações e plano de ação:** há UX e políticas, porém a cobertura de repository/teste ponta a ponta é desigual.
- **CRM, implantação, checklist, governança, qualidade, backup, release notes e alertas:** existem superfícies/tabelas genéricas Enterprise, mas nem todos possuem repository específico, validação e testes de isolamento próprios.
- **Relatórios:** geração e histórico existem, mas Excel real e todos os dez recortes comerciais não estão igualmente implementados.

## Lacunas críticas encontradas e consolidação desta entrega

1. **Cobrança operacional insuficiente:** assinatura continha somente plano/status/datas no DTO. Foram adicionados ciclo, valores, desconto, renovação, vencimento, contatos financeiros, forma de pagamento, observação, bloqueio derivado e pagamentos manuais auditáveis.
2. **Ausência de entidades operacionais dedicadas:** foram adicionadas tabelas idempotentes para campanhas, jobs e central de erros, todas com índices de organização/status/data.
3. **CI incompleto/ausente:** workflow passa a validar Node, Functions, SQL, segredos, restore/build/test/formatação .NET com o SDK definido em `global.json`.
4. **WhatsApp disperso:** foi criado builder central seguro com número oficial, contexto e `correlationId`; somente URLs HTTPS são incluídas.

## Bugs e riscos de produção

- Campanha ainda usa o endpoint de pesquisa na interface V9; até a API de campanhas substituir esse caminho, métricas de público-alvo/pendências não podem ser consideradas fechadas.
- O QR Code da tela V9 depende de serviço público externo; produção deve gerar QR localmente ou contratar provedor com SLA e política de privacidade.
- Não há gateway financeiro: pagamentos são registros operacionais manuais e não conciliação bancária.
- Jobs ganharam contrato de banco, mas ainda não há worker genérico com claim transacional (`FOR UPDATE SKIP LOCKED`), retry/backoff e dead-letter.
- A central de erros ganhou persistência, mas middleware e tela Admin ainda precisam gravar/triá-la.
- Não foi localizada cobertura E2E completa para todos os perfis, unidades e setores; homologação multi-tenant com PostgreSQL real continua obrigatória.

## Rotas, botões e telas que não fecham ponta a ponta

- `Experience/Campaigns`: compartilhar/iniciar/pausar/encerrar funcionam sobre pesquisa; meta, unidade, setor, pendentes e lembrete não possuem controles completos.
- `Experience/Cockpit`: “Gerar relatório” informa preparação, mas usa consulta síncrona e não acompanha um job dedicado.
- Ajuda é busca local; não há backend editorial.
- Módulos baseados em `enterprise_items` não têm sempre uma API tipada por agregado.

## Controles de segurança V10

- Toda operação de assinatura obtém `organization_id` exclusivamente do claim autenticado; o cliente não escolhe o tenant.
- Pagamentos carregam `organization_id` e `registered_by`; consultas filtram obrigatoriamente o tenant.
- Estados e ciclos aceitos são allowlists, valores financeiros são validados e a API exige papel administrativo.
- Payload de job deve conter apenas referências/IDs, nunca credenciais ou dados pessoais. SMTP continua exclusivamente em configuração segura do ambiente.

## Critério de homologação e próximos passos

Esta entrega consolida monetização manual e fundação persistente, mas **não declara o produto integralmente pronto para produção**. Antes da venda: implementar repository/service/API/BFF da nova tabela `campaigns`; worker e tela de jobs; captura da central de erros; downloads assinados de todos os formatos; testes PostgreSQL multi-tenant; QR local; e ensaio de restore/backup. Executar CI e homologar os estados vazio, baixa adesão, bloqueio financeiro, relatório processando e falha com `correlationId` em desktop e mobile.
