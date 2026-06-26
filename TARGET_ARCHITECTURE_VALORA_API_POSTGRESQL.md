# Arquitetura Alvo Valora API/PostgreSQL

A arquitetura alvo mantém Firebase como produção atual e adiciona ASP.NET Core + PostgreSQL em paralelo. O frontend alterna por `DATA_PROVIDER=firebase|api|hybrid`; modo híbrido lê/escreve no primário e compara secundário sem duplicar escrita.
