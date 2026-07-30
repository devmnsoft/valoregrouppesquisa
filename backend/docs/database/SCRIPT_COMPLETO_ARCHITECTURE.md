# Arquitetura do script completo

`script_completo.sql` é a única fonte executável PostgreSQL. Uma transação, advisory lock e `search_path` local protegem a instalação; criação, convergência, seeds e validações são executados atomicamente no schema `valorapesquisa`.
