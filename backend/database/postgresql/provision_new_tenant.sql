-- ============================================================================
-- VALORA INSIGHT™ — SCRIPT OFICIAL DE PROVISIONAMENTO DE NOVO CLIENTE / TENANT
-- SPRINT R9: SEGURANÇA E PROVISIONAMENTO MULTITENANT (ZERO SENHAS ESTÁTICAS)
-- ============================================================================
-- Uso 1 (Função SQL Parametrizada):
--   SELECT * FROM valorapesquisa.provision_tenant(
--       p_org_name    => 'Empresa Piloto Beta',
--       p_org_slug    => 'empresa-piloto-beta',
--       p_plan_code   => 'corporate',
--       p_admin_email => 'admin@pilotobeta.com.br',
--       p_admin_name  => 'Diretor Operacional',
--       p_cnpj        => '11.222.333/0001-81'
--   );
--
-- Uso 2 (Script CLI psql com variáveis de sessão):
--   psql -c "SET valora.provision_org_name = '...'; SET valora.provision_org_slug = '...'; SET valora.provision_plan_code = 'corporate'; SET valora.provision_admin_email = '...'; SET valora.provision_admin_name = '...'; SET valora.provision_cnpj = '...';" -f provision_new_tenant.sql
-- ============================================================================

CREATE OR REPLACE FUNCTION valorapesquisa.validate_cnpj(p_cnpj text)
RETURNS boolean AS $$
DECLARE
    v_clean text;
    v_sum int;
    v_mod int;
    v_d1 int;
    v_d2 int;
    i int;
    v_weights1 int[] := ARRAY[5,4,3,2,9,8,7,6,5,4,3,2];
    v_weights2 int[] := ARRAY[6,5,4,3,2,9,8,7,6,5,4,3,2];
BEGIN
    v_clean := regexp_replace(p_cnpj, '\D', '', 'g');
    IF length(v_clean) <> 14 THEN
        RETURN false;
    END IF;
    -- Rejeitar dígitos todos iguais
    IF v_clean ~ '^(\d)\1{13}$' THEN
        RETURN false;
    END IF;

    -- Primeiro dígito verificador
    v_sum := 0;
    FOR i IN 1..12 LOOP
        v_sum := v_sum + (substring(v_clean, i, 1)::int * v_weights1[i]);
    END LOOP;
    v_mod := v_sum % 11;
    IF v_mod < 2 THEN
        v_d1 := 0;
    ELSE
        v_d1 := 11 - v_mod;
    END IF;

    IF v_d1 <> substring(v_clean, 13, 1)::int THEN
        RETURN false;
    END IF;

    -- Segundo dígito verificador
    v_sum := 0;
    FOR i IN 1..13 LOOP
        v_sum := v_sum + (substring(v_clean, i, 1)::int * v_weights2[i]);
    END LOOP;
    v_mod := v_sum % 11;
    IF v_mod < 2 THEN
        v_d2 := 0;
    ELSE
        v_d2 := 11 - v_mod;
    END IF;

    RETURN v_d2 = substring(v_clean, 14, 1)::int;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

DROP FUNCTION IF EXISTS valorapesquisa.provision_tenant(text,text,text,text,text,text);

CREATE OR REPLACE FUNCTION valorapesquisa.provision_tenant(
    p_org_name text,
    p_org_slug text,
    p_plan_code text,
    p_admin_email text,
    p_admin_name text,
    p_cnpj text
)
RETURNS TABLE (
    out_organization_id uuid,
    out_organization_slug text,
    out_admin_user_id uuid,
    out_admin_email text,
    out_invite_token text,
    out_invite_url text,
    out_plan_code text,
    out_mfa_enforced boolean,
    out_status text
) AS $$
DECLARE
    v_org_id uuid;
    v_user_id uuid;
    v_entity_id uuid;
    v_unit_id uuid;
    v_dept_id uuid;
    v_sub_id uuid;
    v_plan_id uuid;
    v_role_id uuid;
    v_invite_id uuid;
    v_clean_cnpj text;
    v_clean_email text;
    v_clean_slug text;
    v_invite_raw_token text;
    v_invite_token_hash text;
    v_random_pwd_hash text;
    v_existing_org boolean := false;
BEGIN
    -- 1. VALIDAÇÃO DOS DADOS DE ENTRADA
    IF p_org_name IS NULL OR length(trim(p_org_name)) < 3 THEN
        RAISE EXCEPTION 'Nome da organização inválido ou muito curto (mínimo 3 caracteres).';
    END IF;

    v_clean_slug := lower(trim(p_org_slug));
    IF v_clean_slug !~ '^[a-z0-9]+(-[a-z0-9]+)*$' OR length(v_clean_slug) < 3 OR length(v_clean_slug) > 60 THEN
        RAISE EXCEPTION 'Slug inválido ("%"). Deve conter de 3 a 60 caracteres alfanuméricos separados por hífens.', p_org_slug;
    END IF;

    v_clean_email := lower(trim(p_admin_email));
    IF v_clean_email !~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$' THEN
        RAISE EXCEPTION 'E-mail do administrador inválido ("%").', p_admin_email;
    END IF;

    v_clean_cnpj := regexp_replace(p_cnpj, '\D', '', 'g');
    IF NOT valorapesquisa.validate_cnpj(v_clean_cnpj) THEN
        RAISE EXCEPTION 'CNPJ inválido ("%"). Forneça um CNPJ corporativo válido com dígitos verificadores corretos.', p_cnpj;
    END IF;

    -- 2. VALIDAÇÃO ESTRITA DO PLANO COMERCIAL (SEM SUBSTITUIÇÃO SILENCIOSA)
    SELECT p.id INTO v_plan_id 
    FROM valorapesquisa.plans p
    WHERE p.code = lower(trim(p_plan_code)) AND p.is_active = true;

    IF v_plan_id IS NULL THEN
        RAISE EXCEPTION 'Plano comercial especificado ("%") não existe ou está inativo. Provisionamento abortado sem substituição silenciosa de plano.', p_plan_code;
    END IF;

    -- 3. CRIAÇÃO OU RESOLUÇÃO DA ORGANIZAÇÃO (IDEMPOTÊNCIA CONTROLADA)
    SELECT o.id INTO v_org_id FROM valorapesquisa.organizations o WHERE o.slug = v_clean_slug;
    IF v_org_id IS NOT NULL THEN
        v_existing_org := true;
    ELSE
        v_org_id := gen_random_uuid();
        INSERT INTO valorapesquisa.organizations (id, name, slug, status, created_at, updated_at)
        VALUES (v_org_id, trim(p_org_name), v_clean_slug, 'active', now(), now());
    END IF;

    -- 4. ASSINATURA COMERCIAL VINCULADA AO PLANO
    IF NOT EXISTS (SELECT 1 FROM valorapesquisa.subscriptions s WHERE s.organization_id = v_org_id AND s.deleted_at IS NULL) THEN
        v_sub_id := gen_random_uuid();
        INSERT INTO valorapesquisa.subscriptions (
            id, organization_id, plan_id, status, starts_at, billing_cycle, contracted_value, created_at, updated_at
        ) VALUES (
            v_sub_id, v_org_id, v_plan_id, 'active', now(), 'monthly', 1500.00, now(), now()
        );
    END IF;

    -- 5. MÓDULOS CONTRATADOS
    INSERT INTO valorapesquisa.organization_modules (organization_id, module_code, enabled, source, created_at, updated_at)
    VALUES 
        (v_org_id, 'pesquisas', true, 'plan', now(), now()),
        (v_org_id, 'diagnosticos', true, 'plan', now(), now()),
        (v_org_id, 'analytics', true, 'plan', now(), now()),
        (v_org_id, 'planos_acao', true, 'plan', now(), now()),
        (v_org_id, 'benchmarks', true, 'plan', now(), now())
    ON CONFLICT (organization_id, module_code) DO UPDATE SET enabled = true, updated_at = now();

    -- 6. ENTIDADE JURÍDICA (CNPJ)
    IF NOT EXISTS (SELECT 1 FROM valorapesquisa.legal_entities le WHERE le.organization_id = v_org_id) THEN
        v_entity_id := gen_random_uuid();
        INSERT INTO valorapesquisa.legal_entities (id, organization_id, legal_name, trade_name, cnpj, status, created_at, updated_at)
        VALUES (v_entity_id, v_org_id, trim(p_org_name) || ' S.A.', trim(p_org_name), v_clean_cnpj, 'active', now(), now());
    ELSE
        SELECT le.id INTO v_entity_id FROM valorapesquisa.legal_entities le WHERE le.organization_id = v_org_id LIMIT 1;
    END IF;

    -- 7. USUÁRIO ADMINISTRADOR COM ZERO SENHAS ESTÁTICAS E CONVITE SEGURO
    SELECT u.id INTO v_user_id FROM valorapesquisa.users u WHERE u.email = v_clean_email;
    IF v_user_id IS NULL THEN
        v_user_id := gen_random_uuid();
        -- Hash imprevisível gerado dinamicamente via pgcrypto (impede login antes da ativação do convite)
        v_random_pwd_hash := crypt(gen_random_uuid()::text, gen_salt('bf', 12));

        INSERT INTO valorapesquisa.users (
            id, organization_id, email, name, password_hash, status, access_version, password_reset_required, is_deleted, created_at, updated_at
        ) VALUES (
            v_user_id, v_org_id, v_clean_email, trim(p_admin_name), v_random_pwd_hash, 'active', 1, true, false, now(), now()
        );
    END IF;

    -- 8. PAPEL ADMINISTRATIVO (empresa_admin)
    SELECT r.id INTO v_role_id FROM valorapesquisa.roles r WHERE r.code = 'empresa_admin';
    IF v_role_id IS NOT NULL THEN
        INSERT INTO valorapesquisa.user_roles (user_id, role_id, created_at)
        VALUES (v_user_id, v_role_id, now())
        ON CONFLICT (user_id, role_id) DO NOTHING;
    END IF;

    -- 9. GERAÇÃO DE CONVITE CRIPTOGRÁFICO PARA DEFINIÇÃO DE SENHA
    v_invite_raw_token := encode(gen_random_bytes(32), 'hex');
    v_invite_token_hash := encode(sha256(v_invite_raw_token::bytea), 'hex');
    v_invite_id := gen_random_uuid();

    -- Invalida convites pendentes anteriores para o mesmo e-mail nesta organização
    UPDATE valorapesquisa.user_invitations ui
    SET status = 'cancelled', cancelled_at = now(), updated_at = now()
    WHERE ui.organization_id = v_org_id AND ui.normalized_email = v_clean_email AND ui.status = 'pending';

    INSERT INTO valorapesquisa.user_invitations (
        id, organization_id, email, normalized_email, name, token_hash, status, expires_at, invited_by_user_id, resend_count, last_sent_at, created_at, updated_at
    ) VALUES (
        v_invite_id, v_org_id, v_clean_email, v_clean_email, trim(p_admin_name), v_invite_token_hash, 'pending', now() + interval '7 days', v_user_id, 0, now(), now(), now()
    );

    -- 10. UNIDADE E DEPARTAMENTO BASE
    IF NOT EXISTS (SELECT 1 FROM valorapesquisa.units un WHERE un.organization_id = v_org_id) THEN
        v_unit_id := gen_random_uuid();
        INSERT INTO valorapesquisa.units (id, organization_id, legal_entity_id, name, code, status, manager_user_id, created_at, updated_at)
        VALUES (v_unit_id, v_org_id, v_entity_id, 'Matriz Corporativa', 'MAT-01', 'active', v_user_id, now(), now());
    ELSE
        SELECT un.id INTO v_unit_id FROM valorapesquisa.units un WHERE un.organization_id = v_org_id LIMIT 1;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM valorapesquisa.departments d WHERE d.organization_id = v_org_id) THEN
        v_dept_id := gen_random_uuid();
        INSERT INTO valorapesquisa.departments (id, organization_id, unit_id, legal_entity_id, name, code, status, manager_user_id, created_at, updated_at)
        VALUES (v_dept_id, v_org_id, v_unit_id, v_entity_id, 'Administração Geral', 'ADM-01', 'active', v_user_id, now(), now());
    END IF;

    -- 11. POLÍTICA DE SEGURANÇA E MFA OBRIGATÓRIO PARA ADMINS
    INSERT INTO valorapesquisa.organization_settings (
        organization_id, settings, privacy_min_group_size, allow_public_results, allow_certificates, allow_segmentation, public_branding_enabled, metadata_json, created_at, updated_at
    ) VALUES (
        v_org_id,
        '{"security": {"mfa_required_for_admin": true, "session_timeout_minutes": 30, "password_policy": "enterprise"}}'::jsonb,
        5, false, false, false, true,
        jsonb_build_object('mfa_policy', 'enforced_for_admin', 'provisioned_by', 'provision_tenant_service'),
        now(), now()
    ) ON CONFLICT (organization_id) DO UPDATE SET
        settings = valorapesquisa.organization_settings.settings || '{"security": {"mfa_required_for_admin": true}}'::jsonb,
        updated_at = now();

    -- 12. ONBOARDING
    IF NOT EXISTS (SELECT 1 FROM valorapesquisa.organization_onboarding_state oos WHERE oos.organization_id = v_org_id) THEN
        INSERT INTO valorapesquisa.organization_onboarding_state (
            organization_id, hidden, progress_percent, status, version, metadata_json, created_at, updated_at
        ) VALUES (
            v_org_id, false, 20, 'active', 1, '{"currentStep":"welcome"}'::jsonb, now(), now()
        );
    END IF;

    -- 13. AUDITORIA ADMINISTRATIVA
    INSERT INTO valorapesquisa.admin_audit_events (
        organization_id, actor_user_id, event_type, resource_type, resource_id, outcome, metadata_json, occurred_at
    ) VALUES (
        v_org_id, v_user_id, 'tenant.provisioned', 'organization', v_org_id::text, 'success',
        jsonb_build_object(
            'plan_code', p_plan_code,
            'admin_email', v_clean_email,
            'cnpj', v_clean_cnpj,
            'mfa_policy', 'enforced_for_admin',
            'is_existing_org', v_existing_org,
            'invite_id', v_invite_id
        ),
        now()
    );

    RETURN QUERY SELECT 
        v_org_id,
        v_clean_slug,
        v_user_id,
        v_clean_email,
        v_invite_raw_token,
        '/Account/AcceptInvitation?token=' || v_invite_raw_token,
        p_plan_code,
        true,
        CASE WHEN v_existing_org THEN 'existing_tenant_updated'::text ELSE 'new_tenant_provisioned'::text END;
END;
$$ LANGUAGE plpgsql;

-- Execução condicional quando executado via CLI com variáveis de sessão
DO $$
DECLARE
    v_org_name text := current_setting('valora.provision_org_name', true);
    v_org_slug text := current_setting('valora.provision_org_slug', true);
    v_plan_code text := current_setting('valora.provision_plan_code', true);
    v_admin_email text := current_setting('valora.provision_admin_email', true);
    v_admin_name text := current_setting('valora.provision_admin_name', true);
    v_cnpj text := current_setting('valora.provision_cnpj', true);
    v_rec record;
BEGIN
    IF v_org_name IS NOT NULL AND v_org_slug IS NOT NULL AND v_admin_email IS NOT NULL THEN
        SELECT * INTO v_rec FROM valorapesquisa.provision_tenant(
            v_org_name, v_org_slug, COALESCE(v_plan_code, 'corporate'), v_admin_email, COALESCE(v_admin_name, 'Administrador'), COALESCE(v_cnpj, '11.222.333/0001-81')
        );
        RAISE NOTICE 'Provisionamento executado com sucesso: Org % (%) | Admin: % | Token: %', v_rec.out_organization_slug, v_rec.out_organization_id, v_rec.out_admin_email, v_rec.out_invite_token;
    ELSE
        RAISE NOTICE 'Função valorapesquisa.provision_tenant instalada e pronta para chamadas parametrizadas.';
    END IF;
END $$;
