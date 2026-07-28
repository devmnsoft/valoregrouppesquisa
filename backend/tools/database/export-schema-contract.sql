\set ON_ERROR_STOP on
COPY (
  SELECT jsonb_build_object(
    'kind', 'column', 'schema', table_schema, 'table', table_name,
    'name', column_name, 'type', data_type, 'nullable', is_nullable,
    'default', column_default)::text
  FROM information_schema.columns
  WHERE table_schema = 'valorapesquisa' AND table_name <> 'schema_migrations'
  UNION ALL
  SELECT jsonb_build_object(
    'kind', 'constraint', 'schema', n.nspname, 'table', c.relname,
    'name', con.conname, 'definition', pg_get_constraintdef(con.oid))::text
  FROM pg_constraint con
  JOIN pg_class c ON c.oid = con.conrelid
  JOIN pg_namespace n ON n.oid = c.relnamespace
  WHERE n.nspname = 'valorapesquisa' AND c.relname <> 'schema_migrations'
  UNION ALL
  SELECT jsonb_build_object(
    'kind', 'index', 'schema', schemaname, 'table', tablename,
    'name', indexname, 'definition', indexdef)::text
  FROM pg_indexes WHERE schemaname = 'valorapesquisa' AND tablename <> 'schema_migrations'
  ORDER BY 1
) TO STDOUT;
