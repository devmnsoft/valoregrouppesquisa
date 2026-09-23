-- Valora | 36 registros sinteticos em 5 tabelas | baseline 77f0520
-- Script auxiliar de homologacao; nao aplicar automaticamente nem em producao.
-- Conferido contra script_completo.sql. NAO executado em PostgreSQL nesta entrega.
-- Pre-requisitos: schema canonico e DOIS clientes de teste ativos, com dois usuarios
-- ativos .test em cada um, criados pelo fluxo oficial. Nao cria usuarios/senhas/perfis.
-- Os slugs dos clientes devem comecar com seed-valora-.
-- Na MESMA sessao do pgAdmin, antes de executar este arquivo, configurar:
-- SELECT set_config('valora.seed_enabled','on',false);
-- SELECT set_config('valora.seed_database','NOME_EXATO_DO_BANCO_DE_TESTE',false);
-- SELECT set_config('valora.seed_org_a','seed-valora-clinica',false);
-- SELECT set_config('valora.seed_org_b','seed-valora-industria',false);
-- SELECT set_config('valora.seed_owner_email','gestor@clinica.test',false);
-- SELECT set_config('valora.seed_peer_email','analista@clinica.test',false);
-- SELECT set_config('valora.seed_owner_b_email','gestor@industria.test',false);
-- SELECT set_config('valora.seed_peer_b_email','analista@industria.test',false);
-- Nome do banco deve conter segmento test, teste, homolog ou qa, separado por _ ou -.
-- Depois, executar o arquivo completo. Em erro: ROLLBACK; corrigir e repetir.
-- Segunda execucao preserva os mesmos registros e datas, sem resetar edicoes.
-- Nao usar estes dados para maturidade, faturamento, benchmarking ou envio externo.

BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';

DO $seed$
DECLARE
    v_org uuid;
    v_owner uuid;
    v_peer uuid;
    v_workspace uuid;
    v_priority uuid;
    v_i integer;
    v_tenant integer;
    v_slug text;
    v_email text;
    v_peer_email text;
    v_label text;
    v_marker constant text := 'valora-workspace-77f0520-v1';
BEGIN
    IF current_setting('valora.seed_enabled', true) IS DISTINCT FROM 'on'
       OR current_setting('valora.seed_database', true) IS DISTINCT FROM current_database()
       OR current_database() !~* '(^|[_-])(test|teste|homolog|qa)([_-]|$)'
       OR current_database() ~* '(prod|production|producao)'
    THEN
        RAISE EXCEPTION 'Seed bloqueado: habilite explicitamente um banco exclusivo de teste.';
    END IF;
    IF current_setting('valora.seed_org_a',true) IS NULL
       OR current_setting('valora.seed_org_b',true) IS NULL
       OR current_setting('valora.seed_org_a',true) = current_setting('valora.seed_org_b',true)
    THEN RAISE EXCEPTION 'Configure dois clientes de teste distintos.'; END IF;

    PERFORM pg_advisory_xact_lock(hashtext(v_marker));
    FOR v_tenant IN 1..2 LOOP
        v_slug := current_setting(CASE WHEN v_tenant=1 THEN 'valora.seed_org_a' ELSE 'valora.seed_org_b' END,true);
        v_email := current_setting(CASE WHEN v_tenant=1 THEN 'valora.seed_owner_email' ELSE 'valora.seed_owner_b_email' END,true);
        v_peer_email := current_setting(CASE WHEN v_tenant=1 THEN 'valora.seed_peer_email' ELSE 'valora.seed_peer_b_email' END,true);
        IF v_slug NOT LIKE 'seed-valora-%'
           OR v_email IS NULL OR v_peer_email IS NULL
           OR v_email !~* '@[^@]+[.]test$' OR v_peer_email !~* '@[^@]+[.]test$'
           OR lower(v_email)=lower(v_peer_email)
        THEN RAISE EXCEPTION 'Slugs seed-valora- e dois emails distintos .test sao obrigatorios.'; END IF;

        SELECT id INTO STRICT v_org FROM valorapesquisa.organizations
        WHERE slug=v_slug AND status='active' AND deleted_at IS NULL;
        SELECT id INTO STRICT v_owner FROM valorapesquisa.users
        WHERE organization_id=v_org AND lower(email)=lower(v_email) AND status='active' AND deleted_at IS NULL;
        SELECT id INTO STRICT v_peer FROM valorapesquisa.users
        WHERE organization_id=v_org AND lower(email)=lower(v_peer_email) AND status='active' AND deleted_at IS NULL;
        v_label := CASE WHEN v_tenant=1 THEN 'Clinica demonstrativa' ELSE 'Industria demonstrativa' END;

        -- 6 situacoes por cliente: proprio, terceiro, compartilhado, concluido,
        -- cancelado e futuro. Rotas/fontes NULL: nao inventar recursos vinculados.
        FOR v_i IN 1..6 LOOP
            v_workspace := md5(v_marker||':'||v_org::text||':workspace:'||v_i::text)::uuid;
            IF EXISTS(SELECT 1 FROM valorapesquisa.workspace_items WHERE id=v_workspace
                AND (organization_id<>v_org OR metadata->>'fixture' IS DISTINCT FROM v_marker))
            THEN RAISE EXCEPTION 'Colisao com registro nao pertencente ao seed.'; END IF;
            INSERT INTO valorapesquisa.workspace_items
            (id,organization_id,item_type,title,summary,status,priority,due_at,owner_user_id,
             metadata,created_at,updated_at,completed_at)
            VALUES
            (v_workspace,v_org,
             CASE WHEN v_i=1 THEN 'approval' WHEN v_i=3 THEN 'evidence' ELSE 'action' END,
             '[TESTE] '||v_label||' - '||CASE v_i
                WHEN 1 THEN 'Aprovar revisao de atendimento'
                WHEN 2 THEN 'Revisar fluxo de outro responsavel'
                WHEN 3 THEN 'Organizar evidencias compartilhadas'
                WHEN 4 THEN 'Revisao concluida'
                WHEN 5 THEN 'Atividade cancelada'
                ELSE 'Planejar revisao futura' END,
             'Cenario sintetico '||v_i||'. Nao representa cliente real nem resultado metodologico.',
             CASE WHEN v_i=4 THEN 'completed' WHEN v_i=5 THEN 'cancelled' ELSE 'pending' END,
             CASE WHEN v_i=1 THEN 'critical' WHEN v_i=2 THEN 'high' WHEN v_i=6 THEN 'low' ELSE 'medium' END,
             CASE WHEN v_i=6 THEN CURRENT_DATE + INTERVAL '7 days 12 hours'
                  ELSE CURRENT_DATE - INTERVAL '1 day' END,
             CASE WHEN v_i=3 THEN NULL WHEN v_i=2 THEN v_peer ELSE v_owner END,
             jsonb_build_object('fixture',v_marker,'synthetic',true,'scenario',v_i),
             now()-INTERVAL '10 days',now(),CASE WHEN v_i=4 THEN now()-INTERVAL '1 day' ELSE NULL END)
            ON CONFLICT (id) DO NOTHING;
        END LOOP;

        -- 4 prioridades por cliente, com historico coerente e sem indicadores falsos.
        FOR v_i IN 1..4 LOOP
            v_priority := md5(v_marker||':'||v_org::text||':priority:'||v_i::text)::uuid;
            IF EXISTS(SELECT 1 FROM valorapesquisa.executive_priorities WHERE id=v_priority
                AND (organization_id<>v_org OR description IS DISTINCT FROM '[TESTE] '||v_marker))
            THEN RAISE EXCEPTION 'Colisao ou identificacao de fixture modificada em prioridade.'; END IF;
            INSERT INTO valorapesquisa.executive_priorities
            (id,organization_id,title,description,status,priority,owner_user_id,due_at,
             progress_percent,created_by,created_at,updated_at)
            VALUES
            (v_priority,v_org,'[TESTE] '||v_label||' - '||CASE v_i
                WHEN 1 THEN 'Padronizar passagem de turno'
                WHEN 2 THEN 'Revisar responsabilidades'
                WHEN 3 THEN 'Consolidar pendencias da equipe'
                ELSE 'Concluir revisao do procedimento' END,
             '[TESTE] '||v_marker,CASE WHEN v_i=4 THEN 'completed' ELSE 'active' END,
             CASE WHEN v_i=1 THEN 'high' WHEN v_i=2 THEN 'critical' ELSE 'medium' END,
             CASE WHEN v_i=2 THEN v_peer WHEN v_i=3 THEN NULL ELSE v_owner END,
             CURRENT_DATE + INTERVAL '3 days',CASE WHEN v_i=4 THEN 100 ELSE 25 END,
             v_owner,now()-INTERVAL '5 days',now()-INTERVAL '1 day')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO valorapesquisa.executive_priority_updates
            (id,organization_id,priority_id,progress_percent,note,created_by,created_at)
            VALUES
            (md5(v_marker||':'||v_org::text||':update:'||v_i::text)::uuid,
             v_org,v_priority,CASE WHEN v_i=4 THEN 100 ELSE 25 END,
             '[TESTE] Avanco registrado em reuniao simulada.',v_owner,now()-INTERVAL '1 day')
            ON CONFLICT (id) DO NOTHING;
        END LOOP;

        -- Fixados e recentes validos: apenas item proprio e compartilhado.
        FOR v_i IN 1..2 LOOP
            v_workspace := md5(v_marker||':'||v_org::text||':workspace:'||CASE WHEN v_i=1 THEN '1' ELSE '3' END)::uuid;
            INSERT INTO valorapesquisa.user_pinned_items(organization_id,user_id,workspace_item_id)
            VALUES(v_org,v_owner,v_workspace)
            ON CONFLICT(organization_id,user_id,workspace_item_id) DO NOTHING;
            INSERT INTO valorapesquisa.user_recent_items(organization_id,user_id,workspace_item_id,opened_at)
            VALUES(v_org,v_owner,v_workspace,now()-make_interval(hours=>v_i))
            ON CONFLICT(organization_id,user_id,workspace_item_id) DO NOTHING;
        END LOOP;
        RAISE NOTICE 'Cenarios preparados para %: 6 itens, 4 prioridades, 4 atualizacoes, 2 fixados e 2 recentes.',v_slug;
    END LOOP;
END;
$seed$;

-- Resumo limitado as fixtures, sem dados pessoais.
SELECT o.slug, count(*) AS itens_workspace
FROM valorapesquisa.workspace_items w
JOIN valorapesquisa.organizations o ON o.id=w.organization_id
WHERE w.metadata->>'fixture'='valora-workspace-77f0520-v1'
GROUP BY o.slug ORDER BY o.slug;
COMMIT;

-- Esperado, logo apos carga, para o usuario owner de cada cliente:
-- MyDay wide=false: itens 1 e 3; wide=true: 1, 2 e 3.
-- Itens 4 e 5 excluidos por status; item 6 excluido ate o vencimento.
-- Fixados: 2; recentes: 2; prioridades visao ampla: 3 (a concluida e excluida).
-- Nenhum item do outro cliente. Estes totais se referem ao subconjunto do seed.
-- O script nao concede perfis, contratos ou login. Use autenticacao oficial.
-- Para fluxo de pesquisas/respostas, gerar fixtures pelo Application/API apos
-- configurar metodologia e contratos reais. Nao inserir scores arbitrarios.
