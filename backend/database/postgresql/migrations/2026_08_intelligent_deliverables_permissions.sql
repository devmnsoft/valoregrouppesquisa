-- Canonical capabilities for Valora's evidence-based intelligent deliverables.
-- Idempotent and intentionally limited to access data; the intelligence tables are
-- already created by script_completo.sql and remain the canonical persistence model.
BEGIN;

INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
('dashboard.read','Visualizar Valora Dashboard','Consulta a visão executiva baseada em evidências.','organizational_intelligence'),
('radar.read','Visualizar Valora Radar','Consulta o equilíbrio entre dimensões oficiais.','organizational_intelligence'),
('reports.read','Visualizar relatório executivo','Consulta relatórios executivos autorizados.','organizational_intelligence'),
('reports.generate','Gerar relatório executivo','Materializa relatório a partir do diagnóstico.','organizational_intelligence'),
('action.read','Visualizar Valora Action','Consulta planos e ações rastreáveis.','organizational_intelligence'),
('action.manage','Gerenciar Valora Action','Cria e atualiza ações ligadas a evidências.','organizational_intelligence'),
('heatmap.read','Visualizar Valora Heatmap','Consulta concentração interpretada de risco e oportunidade.','organizational_intelligence'),
('evolution.read','Visualizar Valora Evolution','Consulta snapshots históricos sem sobrescrita.','organizational_intelligence'),
('journey.read','Visualizar Valora Journey','Consulta a memória organizacional.','organizational_intelligence'),
('benchmark.read','Visualizar Valora Benchmark','Compara grupos e ciclos, nunca indivíduos.','organizational_intelligence'),
('insights.read','Visualizar Valora Insights IA','Consulta insights sustentados por evidências.','organizational_intelligence')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,
 module_code=EXCLUDED.module_code,updated_at=now();

INSERT INTO valorapesquisa.role_permissions(role_id,permission_id,created_at)
SELECT r.id,p.id,now() FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p
WHERE r.code='admin_valora' AND r.deleted_at IS NULL AND p.code IN
('dashboard.read','radar.read','reports.read','reports.generate','action.read','action.manage','heatmap.read',
 'evolution.read','journey.read','benchmark.read','insights.read')
ON CONFLICT(role_id,permission_id) DO NOTHING;

INSERT INTO valorapesquisa.schema_migrations(version,checksum)
VALUES('2026_08_intelligent_deliverables_permissions','sha256:intelligent-deliverables-permissions-v1')
ON CONFLICT(version) DO UPDATE SET checksum=EXCLUDED.checksum,applied_at=now();

COMMIT;
