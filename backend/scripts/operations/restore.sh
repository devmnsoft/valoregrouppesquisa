#!/usr/bin/env bash
set -euo pipefail

: "${VALORA_RESTORE_DATABASE_URL:?Defina VALORA_RESTORE_DATABASE_URL para um banco vazio de destino}"
dump_file="${1:?Uso: restore.sh CAMINHO.dump [ARQUIVOS.tar.gz]}"
[[ -f "$dump_file" ]] || { echo "Dump não encontrado" >&2; exit 2; }

# A restauração nunca aponta implicitamente para a conexão de produção.
pg_restore --exit-on-error --clean --if-exists --no-owner --no-acl --dbname "$VALORA_RESTORE_DATABASE_URL" "$dump_file"
if [[ $# -ge 2 ]]; then
  : "${VALORA_RESTORE_FILES_DIR:?Defina VALORA_RESTORE_FILES_DIR para restaurar arquivos}"
  mkdir -p "$VALORA_RESTORE_FILES_DIR"
  tar --extract --gzip --file "$2" --directory "$VALORA_RESTORE_FILES_DIR"
fi
printf 'Restauração concluída. Execute os smoke tests antes de promover o ambiente.\n'
