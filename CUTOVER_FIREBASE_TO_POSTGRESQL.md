# Cutover Firebase → PostgreSQL

## Pré-condições

- Produção atual validada com `DATA_PROVIDER=firebase`.
- PostgreSQL migrado localmente e em homologação.
- Comparação Firebase x PostgreSQL sem divergências críticas.
- Backend ASP.NET Core publicado com health checks verdes.
- SMTP/certificados/auditoria validados.

## Passos

1. Congelar janela de escrita ou ativar modo manutenção curto.
2. Executar export Firestore.
3. Transformar dados para contratos PostgreSQL.
4. Importar com `--apply` em PostgreSQL de produção.
5. Rodar comparador Firebase x PostgreSQL.
6. Ativar `DATA_PROVIDER=hybrid` com primário Firebase para observabilidade.
7. Trocar primário para API em homologação controlada.
8. Após aceite, alterar produção para API em janela aprovada.

## Não fazer

- Não enviar segredos SMTP ao frontend.
- Não ativar Cloud Functions em Spark para jornada pública.
- Não duplicar escrita em modo hybrid.
