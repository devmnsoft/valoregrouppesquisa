-- Converges historical names for organizational units into the canonical units.* vocabulary.
-- Safe to execute repeatedly: role links are merged before aliases are removed.
BEGIN;

INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
('units.read','Visualizar unidades','Consulta unidades.','organization'),
('units.create','Criar unidades','Cria unidades.','organization'),
('units.update','Atualizar unidades','Atualiza unidades.','organization'),
('units.disable','Desativar unidades','Desativa unidades.','organization'),
('units.delete','Excluir unidades','Exclui logicamente unidades.','organization')
ON CONFLICT(code) DO UPDATE SET module_code='organization',updated_at=now();

WITH aliases(alias_code, canonical_code) AS (VALUES
  ('organizational_units.read','units.read'), ('organization_units.read','units.read'),
  ('organizational_units.create','units.create'), ('organization_units.create','units.create'),
  ('organizational_units.update','units.update'), ('organization_units.update','units.update'),
  ('organizational_units.disable','units.disable'), ('organization_units.disable','units.disable'),
  ('organizational_units.delete','units.delete'), ('organization_units.delete','units.delete')
)
INSERT INTO valorapesquisa.role_permissions(role_id, permission_id, created_at)
SELECT rp.role_id, canonical.id, rp.created_at
FROM aliases a
JOIN valorapesquisa.permissions legacy ON legacy.code=a.alias_code
JOIN valorapesquisa.permissions canonical ON canonical.code=a.canonical_code
JOIN valorapesquisa.role_permissions rp ON rp.permission_id=legacy.id
ON CONFLICT(role_id,permission_id) DO NOTHING;

WITH aliases(alias_code) AS (VALUES
  ('organizational_units.read'),('organization_units.read'),('organizational_units.create'),('organization_units.create'),
  ('organizational_units.update'),('organization_units.update'),('organizational_units.disable'),('organization_units.disable'),
  ('organizational_units.delete'),('organization_units.delete')
), legacy AS (SELECT id FROM valorapesquisa.permissions WHERE code IN (SELECT alias_code FROM aliases))
DELETE FROM valorapesquisa.permission_migration_reviews WHERE permission_id IN (SELECT id FROM legacy);

WITH aliases(alias_code) AS (VALUES
  ('organizational_units.read'),('organization_units.read'),('organizational_units.create'),('organization_units.create'),
  ('organizational_units.update'),('organization_units.update'),('organizational_units.disable'),('organization_units.disable'),
  ('organizational_units.delete'),('organization_units.delete')
), legacy AS (SELECT id FROM valorapesquisa.permissions WHERE code IN (SELECT alias_code FROM aliases))
DELETE FROM valorapesquisa.role_permissions WHERE permission_id IN (SELECT id FROM legacy);

DELETE FROM valorapesquisa.permissions WHERE code IN (
  'organizational_units.read','organization_units.read','organizational_units.create','organization_units.create',
  'organizational_units.update','organization_units.update','organizational_units.disable','organization_units.disable',
  'organizational_units.delete','organization_units.delete');

INSERT INTO valorapesquisa.schema_migrations(version,checksum)
VALUES('2026_08_canonical_access_permissions','sha256:canonical-access-permissions-v1')
ON CONFLICT(version) DO UPDATE SET checksum=EXCLUDED.checksum,applied_at=now();

COMMIT;
