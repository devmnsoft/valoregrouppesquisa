-- ============================================================================
-- VALORA INSIGHT™ — SANITY CHECK PÓS-IMPLANTAÇÃO / VERIFICAÇÃO DE SAÚDE
-- ============================================================================
-- Executa bateria de checagens de consistência no schema 'valorapesquisa'
-- ============================================================================

DO $$
DECLARE
    v_table_count INT;
    v_plan_count INT;
    v_role_count INT;
    v_admin_count INT;
    v_org_count INT;
    v_broken_fk_count INT;
BEGIN
    RAISE NOTICE 'Iniciando Sanity Check do Valora Insight™...';

    -- 1. Contagem de Tabelas no Schema
    SELECT count(*) INTO v_table_count 
    FROM information_schema.tables 
    WHERE table_schema = 'valorapesquisa';
    
    IF v_table_count < 50 THEN
        RAISE EXCEPTION 'Falha de Sanity Check: Quantidade de tabelas insuficiente no schema valorapesquisa (Encontradas: %)', v_table_count;
    END IF;
    RAISE NOTICE '[OK] Tabelas no schema valorapesquisa: %', v_table_count;

    -- 2. Verificação de Planos Comerciais Ativos
    SELECT count(*) INTO v_plan_count 
    FROM valorapesquisa.plans 
    WHERE is_active = true;
    
    IF v_plan_count = 0 THEN
        RAISE EXCEPTION 'Falha de Sanity Check: Nenhum plano comercial ativo cadastrado.';
    END IF;
    RAISE NOTICE '[OK] Planos comerciais ativos: %', v_plan_count;

    -- 3. Verificação de Papéis e Permissões (RBAC)
    SELECT count(*) INTO v_role_count 
    FROM valorapesquisa.roles;
    
    IF v_role_count < 5 THEN
        RAISE EXCEPTION 'Falha de Sanity Check: Papéis essenciais não encontrados na tabela roles.';
    END IF;
    RAISE NOTICE '[OK] Papéis do sistema configurados: %', v_role_count;

    -- 4. Verificação de Usuários Administradores
    SELECT count(*) INTO v_admin_count 
    FROM valorapesquisa.users u
    JOIN valorapesquisa.user_roles ur ON ur.user_id = u.id
    JOIN valorapesquisa.roles r ON r.id = ur.role_id
    WHERE r.code IN ('admin_valora', 'empresa_admin') AND u.status = 'active';
    
    IF v_admin_count = 0 THEN
        RAISE EXCEPTION 'Falha de Sanity Check: Nenhum usuário administrador ativo encontrado.';
    END IF;
    RAISE NOTICE '[OK] Administradores ativos e vinculados: %', v_admin_count;

    -- 5. Verificação de Extensão pgcrypto / uuid-ossp
    IF NOT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pgcrypto') THEN
        RAISE NOTICE '[AVISO] Extensão pgcrypto não identificada; certifique-se de que gen_random_uuid() está disponível nativamente no PostgreSQL 16.';
    ELSE
        RAISE NOTICE '[OK] Extensão pgcrypto instalada.';
    END IF;

    -- 6. Integridade de Tenants
    SELECT count(*) INTO v_org_count 
    FROM valorapesquisa.organizations 
    WHERE status = 'active';
    RAISE NOTICE '[OK] Organizações ativas prontas para operação: %', v_org_count;

    RAISE NOTICE '=======================================================';
    RAISE NOTICE 'SANITY CHECK CONCLUÍDO COM SUCESSO: AMBIENTE 100%% OPERACIONAL';
    RAISE NOTICE '=======================================================';
END $$;
