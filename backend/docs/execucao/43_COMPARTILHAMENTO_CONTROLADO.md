# Compartilhamento controlado

Foram adicionadas as estruturas canônicas genéricas `share_links` e `share_link_access_events`, com isolamento por organização, hash único do token, escopo, expiração, revogação e trilha de acesso. Tokens puros não são persistidos.

A ativação de links para cada tipo de entregável ainda exige integração do respectivo service/controller; não foram criados botões ou endpoints sem fluxo produtivo.
