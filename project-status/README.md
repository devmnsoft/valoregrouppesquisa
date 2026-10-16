# Acompanhamento do portal Valora

Solicitado pela MNSOFT: inventário de sprints, regras, impedimentos, dúvidas e calendário no portal executivo. Não altera código de negócio.

O workflow project-status.yml executa após push na main e por disparo manual. Gera um JSON público, publica somente na branch portal-status e verifica a gravação. Não executa deploy da aplicação Valora, banco, testes ou envio de mensagens. Usa apenas o token temporário do job com contents:write; não exige credencial de usuário.

O site lê project-status/status.json dessa branch. Uma falha conserva a última referência e deve ser visível. Cache público pode atrasar a leitura. A execução do workflow não significa homologação do produto.

plan.json é a fonte editorial de regras, dúvidas, bloqueios, próximas sprints e última revisão. Datas são propostas, não compromissos. Atualizar esse arquivo com evidências para resolver bloqueios ou alterar aceites. generate.py inventaria todos os Markdown com sprint no nome ou em títulos, sem inferir que foram executados. Datas dos documentos são datas de alteração no Git, não datas de execução de sprint.

Áreas com arquivos alterados desde reviewedCommit ficam como revisão necessária. Sem mudança detectada não equivale a área aprovada. Correspondência por caminho é um sinal inicial e pode precisar de classificação humana. Não usar contagem de commits ou documentos como percentual de conclusão.

Validação local: python3 project-status/generate.py /tmp/status.json. Reaplicação do publicador atualiza o mesmo arquivo, não cria novos registros de negócio. Não rodar publish.py localmente sem autorização e token próprio adequado.
