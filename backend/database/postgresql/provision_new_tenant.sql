-- ============================================================================
-- VALORA INSIGHT™ — SCRIPT OFICIAL DE PROVISIONAMENTO DE NOVO CLIENTE / TENANT
-- ============================================================================
-- Uso:
--   psql -U postgres -d <database> -f provision_new_tenant.sql
--   ou Get-Content provision_new_tenant.sql | docker exec -i sigov-postgres psql -U postgres -d <database>
-- ============================================================================

DO $$
DECLARE
    v_org_id UUID := gen_random_uuid();
    v_user_id UUID := gen_random_uuid();
    v_entity_id UUID := gen_random_uuid();
    v_unit_id UUID := gen_random_uuid();
    v_dept_id UUID := gen_random_uuid();
    v_sub_id UUID := gen_random_uuid();
    v_plan_id UUID;
    v_role_id UUID;

    -- Parâmetros do novo cliente
    c_org_name TEXT := 'Cliente Piloto Oficial';
    c_org_slug TEXT := 'cliente-piloto';
    c_plan_code TEXT := 'corporate';
    c_admin_email TEXT := 'piloto.admin@valora.com.br';
    c_admin_name TEXT := 'Gestor Principal Piloto';
    c_tax_id TEXT := '12.345.678/0001-90';
    -- Hash para senha inicial temporária segura (ex: 'Valora@Pilot2026!')
    c_pwd_hash TEXT := '$2a$12$e8F0s6rLpA7qH.s0H28EuuqC4v18Q3OQh1K0qL2aCj6O1z7G8P5rC';
BEGIN
    -- 1. Criação idempotente da Organização
    IF NOT EXISTS (SELECT 1 FROM valorapesquisa.organizations WHERE slug = c_org_slug) THEN
        INSERT INTO valorapesquisa.organizations (id, name, slug, status, created_at, updated_at)
        VALUES (v_org_id, c_org_name, c_org_slug, 'active', now(), now());
    ELSE
        SELECT id INTO v_org_id FROM valorapesquisa.organizations WHERE slug = c_org_slug;
    END IF;

    -- 2. Obtenção do ID do Plano Comercial
    SELECT id INTO v_plan_id FROM valorapesquisa.plans WHERE code = c_plan_code;
    IF v_plan_id IS NULL THEN
        SELECT id INTO v_plan_id FROM valorapesquisa.plans WHERE is_active = true ORDER BY display_order DESC LIMIT 1;
    END IF;

    -- 3. Atribuição de Assinatura Comercial
    IF NOT EXISTS (SELECT 1 FROM valorapesquisa.subscriptions WHERE organization_id = v_org_id AND deleted_at IS NULL) THEN
        INSERT INTO valorapesquisa.subscriptions (
            id, organization_id, plan_id, status, starts_at, billing_cycle, contracted_value, created_at, updated_at
        ) VALUES (
            v_sub_id, v_org_id, v_plan_id, 'active', now(), 'monthly', 1500.00, now(), now()
        );
    END IF;

    -- 4. Habilitação dos Módulos do Cliente
    INSERT INTO valorapesquisa.organization_modules (organization_id, module_code, enabled, source, created_at, updated_at)
    VALUES 
        (v_org_id, 'pesquisas', true, 'plan', now(), now()),
        (v_org_id, 'diagnosticos', true, 'plan', now(), now()),
        (v_org_id, 'analytics', true, 'plan', now(), now()),
        (v_org_id, 'planos_acao', true, 'plan', now(), now()),
        (v_org_id, 'benchmarks', true, 'plan', now(), now())
    ON CONFLICT (organization_id, module_code) DO UPDATE SET enabled = true, updated_at = now();

    -- 5. Criação da Entidade Jurídica (Empresa/Filial Matriz)
    IF NOT EXISTS (SELECT 1 FROM valorapesquisa.legal_entities WHERE organization_id = v_org_id) THEN
        INSERT INTO valorapesquisa.legal_entities (id, organization_id, legal_name, trade_name, cnpj, status, created_at, updated_at)
        VALUES (v_entity_id, v_org_id, c_org_name || ' S.A.', c_org_name, c_tax_id, 'active', now(), now());
    ELSE
        SELECT id INTO v_entity_id FROM valorapesquisa.legal_entities WHERE organization_id = v_org_id LIMIT 1;
    END IF;

    -- 6. Criação do Usuário Administrador da Organização
    IF NOT EXISTS (SELECT 1 FROM valorapesquisa.users WHERE email = c_admin_email) THEN
        INSERT INTO valorapesquisa.users (
            id, organization_id, email, name, password_hash, status, access_version, password_reset_required, is_deleted, created_at, updated_at
        ) VALUES (
            v_user_id, v_org_id, c_admin_email, c_admin_name, c_pwd_hash, 'active', 1, false, false, now(), now()
        );
    ELSE
        SELECT id INTO v_user_id FROM valorapesquisa.users WHERE email = c_admin_email;
    END IF;

    -- 7. Atribuição de Perfil / Papel (empresa_admin)
    SELECT id INTO v_role_id FROM valorapesquisa.roles WHERE code = 'empresa_admin';
    IF v_role_id IS NOT NULL THEN
        INSERT INTO valorapesquisa.user_roles (user_id, role_id, created_at)
        VALUES (v_user_id, v_role_id, now())
        ON CONFLICT (user_id, role_id) DO NOTHING;
    END IF;

    -- 8. Criação da Unidade Operacional Matriz
    IF NOT EXISTS (SELECT 1 FROM valorapesquisa.units WHERE organization_id = v_org_id) THEN
        INSERT INTO valorapesquisa.units (id, organization_id, legal_entity_id, name, code, status, manager_user_id, created_at, updated_at)
        VALUES (v_unit_id, v_org_id, v_entity_id, 'Matriz Corporativa', 'MAT-01', 'active', v_user_id, now(), now());
    ELSE
        SELECT id INTO v_unit_id FROM valorapesquisa.units WHERE organization_id = v_org_id LIMIT 1;
    END IF;

    -- 9. Criação do Departamento Padrão
    IF NOT EXISTS (SELECT 1 FROM valorapesquisa.departments WHERE organization_id = v_org_id) THEN
        INSERT INTO valorapesquisa.departments (id, organization_id, unit_id, legal_entity_id, name, code, status, manager_user_id, created_at, updated_at)
        VALUES (v_dept_id, v_org_id, v_unit_id, v_entity_id, 'Administração Geral', 'ADM-01', 'active', v_user_id, now(), now());
    END IF;

    -- 10. Inicialização do Onboarding Guiado
    IF NOT EXISTS (SELECT 1 FROM valorapesquisa.organization_onboarding_state WHERE organization_id = v_org_id) THEN
        INSERT INTO valorapesquisa.organization_onboarding_state (
            organization_id, hidden, progress_percent, status, version, metadata_json, created_at, updated_at
        ) VALUES (
            v_org_id, false, 20, 'active', 1, '{"currentStep":"welcome"}'::jsonb, now(), now()
        );
    END IF;

    RAISE NOTICE 'Provisionamento concluído com sucesso: Org ID: %, Slug: %, Admin: % (ID: %)', v_org_id, c_org_slug, c_admin_email, v_user_id;
END $$;
