-- Base metodológica oficial Valora Insight™. Aditiva, idempotente e sem recálculo histórico.
BEGIN;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE SCHEMA IF NOT EXISTS valorapesquisa;

CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_versions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code varchar(30) NOT NULL UNIQUE, version integer NOT NULL,
 name varchar(160) NOT NULL, status varchar(20) NOT NULL CHECK(status IN ('draft','active','retired')),
 effective_from timestamptz NOT NULL, effective_to timestamptz, change_log text NOT NULL DEFAULT '',
 snapshot_json jsonb NOT NULL DEFAULT '{}'::jsonb, published_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(),
 CHECK(effective_to IS NULL OR effective_to > effective_from));

CREATE TABLE IF NOT EXISTS valorapesquisa.maturity_dimensions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 code varchar(80) NOT NULL, name varchar(160) NOT NULL, description text NOT NULL, weight numeric(8,4) NOT NULL DEFAULT 1,
 status varchar(20) NOT NULL DEFAULT 'active' CHECK(status IN ('draft','active','inactive')),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(methodology_version_id,code), CHECK(weight>0));

CREATE TABLE IF NOT EXISTS valorapesquisa.cognitive_concepts (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 code varchar(80) NOT NULL, name varchar(180) NOT NULL, description text NOT NULL, primary_dimension_id uuid NOT NULL REFERENCES valorapesquisa.maturity_dimensions(id),
 related_dimension_ids uuid[] NOT NULL DEFAULT '{}', methodological_definition text NOT NULL, expected_evidence jsonb NOT NULL DEFAULT '[]',
 low_maturity_signs jsonb NOT NULL DEFAULT '[]', medium_maturity_signs jsonb NOT NULL DEFAULT '[]', high_maturity_signs jsonb NOT NULL DEFAULT '[]',
 associated_risks jsonb NOT NULL DEFAULT '[]', associated_opportunities jsonb NOT NULL DEFAULT '[]', possible_recommendations jsonb NOT NULL DEFAULT '[]',
 status varchar(20) NOT NULL DEFAULT 'active' CHECK(status IN ('draft','active','inactive')), version integer NOT NULL DEFAULT 1,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(methodology_version_id,code));

CREATE TABLE IF NOT EXISTS valorapesquisa.cognitive_concept_relations (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 source_concept_id uuid NOT NULL REFERENCES valorapesquisa.cognitive_concepts(id), target_concept_id uuid NOT NULL REFERENCES valorapesquisa.cognitive_concepts(id),
 relation_type varchar(30) NOT NULL CHECK(relation_type IN ('probable_cause','impact','dependency','correlation','aggravating','mitigating','prerequisite')),
 intensity numeric(5,4) NOT NULL CHECK(intensity>0 AND intensity<=1), direction varchar(20) NOT NULL CHECK(direction IN ('positive','negative','bidirectional')),
 description text NOT NULL, interpretation_rule jsonb NOT NULL DEFAULT '{}', version integer NOT NULL DEFAULT 1,
 created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(methodology_version_id,source_concept_id,target_concept_id,relation_type));

CREATE TABLE IF NOT EXISTS valorapesquisa.official_questions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 code varchar(80) NOT NULL, text text NOT NULL, internal_description text NOT NULL DEFAULT '',
 response_type varchar(30) NOT NULL CHECK(response_type IN ('scale_1_5','multiple_choice','yes_no','qualitative_text','matrix','single_choice')),
 scale_json jsonb NOT NULL DEFAULT '{}', dimension_id uuid NOT NULL REFERENCES valorapesquisa.maturity_dimensions(id),
 primary_concept_id uuid NOT NULL REFERENCES valorapesquisa.cognitive_concepts(id), weight numeric(8,4) NOT NULL DEFAULT 1 CHECK(weight>0),
 is_required boolean NOT NULL DEFAULT true, target_audience text[] NOT NULL DEFAULT '{}', assessed_maturity_level varchar(30),
 normalization_rule jsonb NOT NULL, status varchar(20) NOT NULL DEFAULT 'active' CHECK(status IN ('draft','active','inactive')),
 version integer NOT NULL DEFAULT 1, effective_from timestamptz NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(methodology_version_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.official_question_options (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), question_id uuid NOT NULL REFERENCES valorapesquisa.official_questions(id) ON DELETE CASCADE,
 code varchar(50) NOT NULL, label text NOT NULL, normalized_value numeric(7,4), display_order integer NOT NULL DEFAULT 0,
 UNIQUE(question_id,code), CHECK(normalized_value IS NULL OR normalized_value BETWEEN 0 AND 100));

-- Corrige resíduos antes de fortalecer o contrato da tabela legada.
DO $mapping$ BEGIN
 IF to_regclass('valorapesquisa.question_concept_mappings') IS NOT NULL THEN
  UPDATE valorapesquisa.question_concept_mappings SET weight=1 WHERE weight IS NULL OR weight<=0;
  ALTER TABLE valorapesquisa.question_concept_mappings DROP CONSTRAINT IF EXISTS question_concept_mappings_weight_check;
  ALTER TABLE valorapesquisa.question_concept_mappings ADD CONSTRAINT question_concept_mappings_weight_check CHECK(weight>0) NOT VALID;
  ALTER TABLE valorapesquisa.question_concept_mappings VALIDATE CONSTRAINT question_concept_mappings_weight_check;
 END IF;
END $mapping$;
CREATE TABLE IF NOT EXISTS valorapesquisa.official_question_concepts (
 question_id uuid NOT NULL REFERENCES valorapesquisa.official_questions(id) ON DELETE CASCADE,
 concept_id uuid NOT NULL REFERENCES valorapesquisa.cognitive_concepts(id), weight numeric(8,4) NOT NULL DEFAULT 1 CHECK(weight>0),
 is_primary boolean NOT NULL DEFAULT false, PRIMARY KEY(question_id,concept_id));

CREATE TABLE IF NOT EXISTS valorapesquisa.scoring_rules (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 code varchar(80) NOT NULL, name varchar(180) NOT NULL, rule_json jsonb NOT NULL, status varchar(20) NOT NULL DEFAULT 'active',
 version integer NOT NULL DEFAULT 1, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(methodology_version_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.maturity_levels (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 code varchar(30) NOT NULL, name varchar(100) NOT NULL, minimum_score numeric(5,2) NOT NULL, maximum_score numeric(5,2) NOT NULL,
 description text NOT NULL, organizational_meaning text NOT NULL, typical_risks jsonb NOT NULL DEFAULT '[]', recommended_next_step text NOT NULL,
 display_order integer NOT NULL, UNIQUE(methodology_version_id,code), CHECK(minimum_score>=0 AND maximum_score<=100 AND maximum_score>minimum_score));
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnosis_templates (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 code varchar(80) NOT NULL, name varchar(180) NOT NULL, audience text[] NOT NULL DEFAULT '{}', estimated_minutes integer NOT NULL CHECK(estimated_minutes>0),
 minimum_plan varchar(40) NOT NULL, enabled_deliverables jsonb NOT NULL DEFAULT '[]', scoring_rule_id uuid NOT NULL REFERENCES valorapesquisa.scoring_rules(id),
 dimensions_json jsonb NOT NULL DEFAULT '[]', status varchar(20) NOT NULL DEFAULT 'active', version integer NOT NULL DEFAULT 1,
 created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(methodology_version_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnosis_template_questions (
 template_id uuid NOT NULL REFERENCES valorapesquisa.diagnosis_templates(id) ON DELETE CASCADE,
 question_id uuid NOT NULL REFERENCES valorapesquisa.official_questions(id), display_order integer NOT NULL, is_required boolean NOT NULL DEFAULT true,
 PRIMARY KEY(template_id,question_id), UNIQUE(template_id,display_order));

-- Snapshot imutável: o diagnóstico publicado aponta para uma versão e seu conteúdo materializado.
DO $diagnosis_version$ BEGIN
 IF to_regclass('valorapesquisa.surveys') IS NOT NULL THEN
  ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS methodology_version_id uuid REFERENCES valorapesquisa.methodology_versions(id);
  ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS methodology_snapshot_json jsonb;
  ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS diagnosis_template_id uuid REFERENCES valorapesquisa.diagnosis_templates(id);
 END IF;
END $diagnosis_version$;

CREATE TABLE IF NOT EXISTS valorapesquisa.evidence_items_methodology (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), diagnosis_id uuid NOT NULL, question_id uuid NOT NULL REFERENCES valorapesquisa.official_questions(id),
 answer_id uuid NOT NULL, dimension_id uuid NOT NULL REFERENCES valorapesquisa.maturity_dimensions(id), concept_id uuid NOT NULL REFERENCES valorapesquisa.cognitive_concepts(id),
 intensity numeric(7,4) NOT NULL CHECK(intensity BETWEEN 0 AND 100), polarity varchar(10) NOT NULL CHECK(polarity IN ('positive','negative','neutral')),
 confidence numeric(5,4) NOT NULL CHECK(confidence BETWEEN 0 AND 1), interpretation text NOT NULL, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.recommendation_catalog (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 code varchar(80) NOT NULL, concept_id uuid NOT NULL REFERENCES valorapesquisa.cognitive_concepts(id), dimension_id uuid NOT NULL REFERENCES valorapesquisa.maturity_dimensions(id),
 trigger_condition jsonb NOT NULL CHECK(trigger_condition<>'{}'::jsonb), priority varchar(20) NOT NULL, description text NOT NULL, objective text NOT NULL,
 prerequisites jsonb NOT NULL DEFAULT '[]', mitigated_risks jsonb NOT NULL DEFAULT '[]', success_indicators jsonb NOT NULL DEFAULT '[]', suggested_actions jsonb NOT NULL DEFAULT '[]',
 status varchar(20) NOT NULL DEFAULT 'active', version integer NOT NULL DEFAULT 1, UNIQUE(methodology_version_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.recommendation_evidence (
 recommendation_id uuid NOT NULL REFERENCES valorapesquisa.recommendation_catalog(id), evidence_id uuid NOT NULL REFERENCES valorapesquisa.evidence_items_methodology(id),
 created_at timestamptz NOT NULL DEFAULT now(), PRIMARY KEY(recommendation_id,evidence_id));

INSERT INTO valorapesquisa.methodology_versions(code,version,name,status,effective_from,published_at,change_log)
VALUES('VALORA-2026.1',1,'Metodologia Valora Insight™ 2026.1','active','2026-01-01',now(),'Base cognitiva oficial inicial.') ON CONFLICT(code) DO NOTHING;
WITH v AS (SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1')
INSERT INTO valorapesquisa.maturity_dimensions(methodology_version_id,code,name,description,weight)
SELECT v.id,x.code,x.name,x.description,1 FROM v CROSS JOIN (VALUES
 ('clarity','Clareza Sistêmica','Propósito, papéis, responsabilidades e interfaces explícitos.'),('governance','Governança','Decisões, accountability, riscos e indicadores.'),
 ('leadership','Liderança','Contexto, direção e desenvolvimento.'),('culture_people','Cultura e Pessoas','Padrões, comunicação e capacidade humana.'),
 ('process_systems','Processos e Sistemas','Fluxos, tecnologia, repetibilidade e integração.'),('intelligence_learning','Inteligência e Aprendizagem','Evidências convertidas em decisão e evolução.'),
 ('sustainability','Sustentabilidade','Autonomia, resiliência e continuidade organizacional.')) x(code,name,description)
ON CONFLICT(methodology_version_id,code) DO UPDATE SET name=excluded.name,description=excluded.description,updated_at=now();
WITH v AS (SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1'), concepts(code,name,dimension,definition) AS (VALUES
 ('systemic_clarity','Clareza sistêmica','clarity','Compreensão compartilhada de propósito, papéis, critérios e interfaces.'),
 ('organizational_governance','Governança organizacional','governance','Sistema de direção, decisão, prestação de contas e supervisão.'),('leadership','Liderança','leadership','Capacidade de produzir contexto, direção e desenvolvimento.'),
 ('organizational_culture','Cultura organizacional','culture_people','Padrões compartilhados que orientam comportamentos.'),('people','Pessoas','culture_people','Condições para contribuição, desenvolvimento e pertencimento.'),
 ('processes','Processos','process_systems','Fluxos de valor explícitos, medidos e aprimorados.'),('systems','Sistemas','process_systems','Recursos técnicos e sociais integrados ao trabalho.'),
 ('organizational_learning','Aprendizagem organizacional','intelligence_learning','Capacidade de aprender com ciclos e evidências.'),('organizational_intelligence','Inteligência organizacional','intelligence_learning','Capacidade de converter evidência em decisão melhor.'),
 ('organizational_sustainability','Sustentabilidade organizacional','sustainability','Capacidade de sustentar resultados e adaptação no tempo.'),('organizational_autonomy','Autonomia organizacional','sustainability','Decisão distribuída com contexto, limites e responsabilidade.'),
 ('key_person_dependency','Dependência de pessoas específicas','sustainability','Concentração crítica de conhecimento ou decisão.'),('decision_making','Tomada de decisão','governance','Escolha rastreável por critérios e evidências.'),
 ('internal_communication','Comunicação interna','culture_people','Fluxo confiável de contexto, acordos e feedback.'),('indicators','Indicadores','intelligence_learning','Evidências quantitativas e qualitativas interpretadas em contexto.'),
 ('accountability','Accountability','governance','Assumir, prestar contas e aprender sobre compromissos.'),('organizational_development','Desenvolvimento organizacional','intelligence_learning','Transformação planejada da arquitetura organizacional.'))
INSERT INTO valorapesquisa.cognitive_concepts(methodology_version_id,code,name,description,primary_dimension_id,methodological_definition,expected_evidence,low_maturity_signs,medium_maturity_signs,high_maturity_signs,associated_risks,associated_opportunities,possible_recommendations)
SELECT v.id,c.code,c.name,c.definition,d.id,c.definition,'["práticas observáveis","registros recorrentes"]','["prática informal ou dependente"]','["prática definida, ainda irregular"]','["prática integrada, medida e aprendida"]','["descontinuidade","decisão sem evidência"]','["integração","aprendizagem"]','["instituir ciclo com responsável, indicador e revisão"]'
FROM v JOIN concepts c ON true JOIN valorapesquisa.maturity_dimensions d ON d.methodology_version_id=v.id AND d.code=c.dimension
ON CONFLICT(methodology_version_id,code) DO UPDATE SET name=excluded.name,methodological_definition=excluded.methodological_definition,updated_at=now();
WITH v AS (SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1')
INSERT INTO valorapesquisa.maturity_levels(methodology_version_id,code,name,minimum_score,maximum_score,description,organizational_meaning,typical_risks,recommended_next_step,display_order)
SELECT v.id,x.* FROM v CROSS JOIN (VALUES
 ('initial','Inicial',0,19.99,'Práticas incipientes.','Alta dependência de iniciativas isoladas.','["descontinuidade"]','Estabelecer fundamentos explícitos.',1),
 ('structuring','Em estruturação',20,39.99,'Fundamentos em definição.','Existem iniciativas ainda pouco integradas.','["fragmentação"]','Formalizar papéis e rotinas.',2),
 ('developing','Em desenvolvimento',40,59.99,'Práticas em adoção.','Capacidades evoluem com consistência variável.','["execução irregular"]','Medir adoção e remover barreiras.',3),
 ('consistent','Consistente',60,74.99,'Práticas recorrentes.','A organização opera com previsibilidade.','["acomodação"]','Integrar capacidades e aprendizagem.',4),
 ('mature','Madura',75,89.99,'Práticas integradas.','Decisões e resultados são sustentáveis.','["otimização local"]','Ampliar adaptação sistêmica.',5),
 ('intelligent','Inteligente',90,100,'Práticas adaptativas.','O sistema aprende e evolui por evidências.','["excesso de confiança"]','Preservar aprendizagem e renovação.',6)) x(code,name,min,max,description,meaning,risks,next_step,display_order)
ON CONFLICT(methodology_version_id,code) DO UPDATE SET name=excluded.name,minimum_score=excluded.minimum_score,maximum_score=excluded.maximum_score;
WITH v AS (SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1')
INSERT INTO valorapesquisa.scoring_rules(methodology_version_id,code,name,rule_json)
SELECT id,'weighted-evidence-v1','Scoring ponderado por evidências','{"scale":"0-100","aggregation":"weighted_average","invalid":"ignore","zeroDenominator":"insufficient_evidence","confidence":"valid_required_ratio"}' FROM v ON CONFLICT(methodology_version_id,code) DO NOTHING;
WITH v AS (SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1'), r AS (SELECT id FROM valorapesquisa.scoring_rules WHERE code='weighted-evidence-v1'), templates(code,name,audience,minutes,plan,deliverables) AS (VALUES
 ('essential','Diagnóstico Essencial',ARRAY['equipes'],15,'Free','["score","radar"]'::jsonb),('professional','Diagnóstico Profissional',ARRAY['organização'],30,'Professional','["score","radar","heatmap","action_plan"]'),
 ('executive','Diagnóstico Executivo',ARRAY['alta liderança'],25,'Professional','["executive_report","benchmark"]'),('leadership','Diagnóstico de Liderança',ARRAY['líderes'],20,'Professional','["score","insights"]'),
 ('governance','Diagnóstico de Governança',ARRAY['governança'],25,'Professional','["score","risks","recommendations"]'),('culture','Diagnóstico de Cultura',ARRAY['organização'],25,'Professional','["score","heatmap"]'),
 ('enterprise_units','Diagnóstico Enterprise por unidades',ARRAY['múltiplas unidades'],45,'Enterprise','["score","radar","heatmap","benchmark","action_plan","certificate"]'))
INSERT INTO valorapesquisa.diagnosis_templates(methodology_version_id,code,name,audience,estimated_minutes,minimum_plan,enabled_deliverables,scoring_rule_id)
SELECT v.id,t.code,t.name,t.audience,t.minutes,t.plan,t.deliverables,r.id FROM v CROSS JOIN r CROSS JOIN templates t
ON CONFLICT(methodology_version_id,code) DO UPDATE SET name=excluded.name,estimated_minutes=excluded.estimated_minutes,enabled_deliverables=excluded.enabled_deliverables;

-- Uma pergunta oficial inicial por conceito garante cobertura canônica; novas versões são inseridas, nunca sobrescritas.
WITH v AS (SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1')
INSERT INTO valorapesquisa.official_questions(methodology_version_id,code,text,internal_description,response_type,scale_json,dimension_id,primary_concept_id,weight,is_required,target_audience,normalization_rule,effective_from)
SELECT v.id,'VALORA_'||upper(c.code)||'_01','Em que medida '||lower(c.name)||' está formalizada, é praticada e revisada com evidências?',
 'Item basal oficial de '||c.name||'.','scale_1_5','{"minimum":1,"maximum":5,"labels":{"1":"não existe","5":"integrada e adaptativa"}}',c.primary_dimension_id,c.id,1,true,ARRAY['organização'],
 '{"type":"linear","minimum":1,"maximum":5,"outputMinimum":0,"outputMaximum":100}',v.effective_from
FROM v JOIN valorapesquisa.cognitive_concepts c ON c.methodology_version_id=v.id
ON CONFLICT(methodology_version_id,code) DO UPDATE SET text=excluded.text,internal_description=excluded.internal_description;
INSERT INTO valorapesquisa.official_question_concepts(question_id,concept_id,weight,is_primary)
SELECT q.id,q.primary_concept_id,1,true FROM valorapesquisa.official_questions q
ON CONFLICT(question_id,concept_id) DO UPDATE SET weight=1,is_primary=true;
WITH edges(source,target,type,direction,description) AS (VALUES
 ('systemic_clarity','organizational_governance','impact','positive','Baixa clareza sistêmica fragiliza a governança.'),
 ('organizational_governance','key_person_dependency','impact','negative','Governança fraca amplia dependência de pessoas específicas.'),
 ('indicators','decision_making','prerequisite','positive','Indicadores contextualizados qualificam a decisão.'),
 ('leadership','organizational_autonomy','impact','positive','Liderança que distribui contexto fortalece autonomia.'))
INSERT INTO valorapesquisa.cognitive_concept_relations(methodology_version_id,source_concept_id,target_concept_id,relation_type,intensity,direction,description,interpretation_rule)
SELECT s.methodology_version_id,s.id,t.id,e.type,.8,e.direction,e.description,'{"minimumEvidence":3,"causality":"hypothesis_only"}'
FROM edges e JOIN valorapesquisa.cognitive_concepts s ON s.code=e.source JOIN valorapesquisa.cognitive_concepts t ON t.code=e.target AND t.methodology_version_id=s.methodology_version_id
ON CONFLICT(methodology_version_id,source_concept_id,target_concept_id,relation_type) DO UPDATE SET description=excluded.description,interpretation_rule=excluded.interpretation_rule;
WITH v AS (SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1')
INSERT INTO valorapesquisa.recommendation_catalog(methodology_version_id,code,concept_id,dimension_id,trigger_condition,priority,description,objective,prerequisites,mitigated_risks,success_indicators,suggested_actions)
SELECT v.id,'REC_'||upper(c.code)||'_FOUNDATION',c.id,c.primary_dimension_id,'{"conceptScore":{"lessThan":60},"minimumEvidence":1}','high',
 'Estruturar '||lower(c.name)||' com responsabilidade e cadência de revisão.','Elevar a maturidade observável de '||lower(c.name)||'.','["responsável definido"]','["descontinuidade","dependência"]','["score do conceito","evidências recorrentes"]','["definir prática","registrar evidência","revisar resultado"]'
FROM v JOIN valorapesquisa.cognitive_concepts c ON c.methodology_version_id=v.id
ON CONFLICT(methodology_version_id,code) DO UPDATE SET trigger_condition=excluded.trigger_condition,description=excluded.description;

-- Catálogo fechado e concessão integral ao admin_valora.
INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
 ('methodology.read','Visualizar metodologia','Consulta a metodologia oficial versionada.','organizational_intelligence'),
 ('methodology.manage','Gerenciar metodologia','Publica e versiona a metodologia oficial.','organizational_intelligence'),
 ('dimensions.manage','Gerenciar dimensões','Administra dimensões metodológicas.','organizational_intelligence'),
 ('concepts.manage','Gerenciar conceitos','Administra o dicionário cognitivo.','organizational_intelligence'),
 ('cognitive_map.manage','Gerenciar mapa cognitivo','Administra relações cognitivas.','organizational_intelligence'),
 ('official_questions.manage','Gerenciar perguntas oficiais','Administra perguntas versionadas.','forms'),
 ('diagnosis_templates.manage','Gerenciar templates diagnósticos','Administra templates oficiais.','forms'),
 ('scoring_rules.manage','Gerenciar regras de score','Administra regras versionadas de scoring.','results'),
 ('maturity_levels.manage','Gerenciar níveis de maturidade','Administra faixas oficiais.','results'),
 ('recommendations.manage','Gerenciar recomendações','Administra catálogo baseado em evidências.','results')
ON CONFLICT(code) DO UPDATE SET name=excluded.name,description=excluded.description,module_code=excluded.module_code,updated_at=now();
INSERT INTO valorapesquisa.role_permissions(role_id,permission_id)
SELECT r.id,p.id FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p
WHERE r.code='admin_valora' AND p.code IN ('methodology.read','methodology.manage','dimensions.manage','concepts.manage','cognitive_map.manage','official_questions.manage','diagnosis_templates.manage','scoring_rules.manage','maturity_levels.manage','recommendations.manage')
ON CONFLICT(role_id,permission_id) DO NOTHING;

COMMIT;
