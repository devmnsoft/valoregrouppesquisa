# Platform Admin e System Health — execução 53

O health administrativo existente foi exposto pelo BFF e ganhou uma página real em `/SystemHealth`. A API apresenta ambiente, versão, validação de configuração, saúde de schema, backup e manutenção sem retornar valores secretos. Eventos detalhados continuam disponíveis apenas para `platform_admin`.

O console global de alteração de plano/status não foi declarado pronto: faltam homologação transacional e justificativa obrigatória em banco real. Não foram criados botões para essas ações enquanto o fluxo não estiver seguro.
