using Dapper;
using Valora.Application.CompanyRegistration;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Repositories;

public sealed class CompanyRegistrationRepository : ICompanyRegistrationRepository
{
    public async Task<RegisterCompanyResult> RegisterAsync(IUnitOfWork uow, RegisterCompanyCommand c)
    {
        var existing = await uow.Connection.QuerySingleOrDefaultAsync<(string RequestHash, Guid? OrganizationId)>(
            "SELECT request_hash,organization_id FROM idempotency_keys WHERE key=@IdempotencyKey FOR UPDATE",
            c, uow.Transaction);
        if (existing.OrganizationId is not null)
        {
            if (!string.Equals(existing.RequestHash, c.RequestHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Idempotency key já utilizada com outro conteúdo.");
            return await LoadAsync(uow, existing.OrganizationId.Value, true);
        }

        await uow.Connection.ExecuteAsync("INSERT INTO idempotency_keys(key,request_hash,expires_at) VALUES(@IdempotencyKey,@RequestHash,now()+interval '24 hours')", c, uow.Transaction);
        var organizationId = await uow.Connection.ExecuteScalarAsync<Guid>("INSERT INTO organizations(name,public_name,email,phone,slug,default_language_code,time_zone,onboarding_status) VALUES(@CompanyName,@TradeName,@AdministratorEmail,@Phone,lower(regexp_replace(@CompanyName,'[^a-zA-Z0-9]+','-','g'))||'-'||substr(gen_random_uuid()::text,1,6),@Language,@TimeZone,'in_progress') RETURNING id", c, uow.Transaction);
        var legalEntityId = await uow.Connection.ExecuteScalarAsync<Guid>("INSERT INTO legal_entities(organization_id,legal_name,trade_name,cnpj) VALUES(@organizationId,@CompanyName,@TradeName,@Cnpj) RETURNING id", new { organizationId, c.CompanyName, c.TradeName, c.Cnpj }, uow.Transaction);
        await uow.Connection.ExecuteAsync("INSERT INTO addresses(organization_id,legal_entity_id,address_type) VALUES(@organizationId,@legalEntityId,'headquarters')", new { organizationId, legalEntityId }, uow.Transaction);
        var unitId = await uow.Connection.ExecuteScalarAsync<Guid>("INSERT INTO units(organization_id,legal_entity_id,name,code) VALUES(@organizationId,@legalEntityId,COALESCE(@TradeName,@CompanyName),'MAIN') RETURNING id", new { organizationId, legalEntityId, c.TradeName, c.CompanyName }, uow.Transaction);
        var userId = await uow.Connection.ExecuteScalarAsync<Guid>("INSERT INTO users(organization_id,email,name,password_hash,phone) VALUES(@organizationId,@AdministratorEmail,@AdministratorName,@PasswordHash,@Phone) RETURNING id", new { organizationId, c.AdministratorEmail, c.AdministratorName, c.PasswordHash, c.Phone }, uow.Transaction);
        await uow.Connection.ExecuteAsync("INSERT INTO user_roles(user_id,role_id) SELECT @userId,id FROM roles WHERE code='empresa_admin' AND organization_id IS NULL; INSERT INTO user_scopes(user_id,organization_id) VALUES(@userId,@organizationId); INSERT INTO user_scopes(user_id,organization_id,legal_entity_id) VALUES(@userId,@organizationId,@legalEntityId); INSERT INTO user_scopes(user_id,organization_id,legal_entity_id,unit_id) VALUES(@userId,@organizationId,@legalEntityId,@unitId); INSERT INTO subscriptions(organization_id,plan_id,status) SELECT @organizationId,id,'active' FROM plans WHERE code='free'; INSERT INTO organization_settings(organization_id) VALUES(@organizationId); INSERT INTO organization_branding(organization_id) VALUES(@organizationId);", new { organizationId, legalEntityId, unitId, userId }, uow.Transaction);
        foreach (var step in new[] { "company_registered", "cnpj_validated", "main_unit_created", "administrator_created", "profile_review", "team_invitation", "first_survey", "ready" })
            await uow.Connection.ExecuteAsync("INSERT INTO onboarding_steps(organization_id,step_code,status,completed_at) VALUES(@organizationId,@step,@status,CASE WHEN @status='completed' THEN now() END)", new { organizationId, step, status = step is "company_registered" or "cnpj_validated" or "main_unit_created" or "administrator_created" ? "completed" : "pending" }, uow.Transaction);
        await uow.Connection.ExecuteAsync("INSERT INTO organization_consents(organization_id,user_id,consent_type,version,ip_hash) VALUES(@organizationId,@userId,'terms_of_use','1',@ConsentIpHash),(@organizationId,@userId,'privacy_policy','1',@ConsentIpHash); INSERT INTO audit_logs(organization_id,user_id,action,entity_type,entity_id,message,metadata_json) VALUES(@organizationId,@userId,'company.registration.completed','organization',CAST(@organizationId AS text),'Cadastro empresarial concluído','{}'::jsonb); INSERT INTO outbox_messages(aggregate_id,message_type,payload,idempotency_key) VALUES(@organizationId,'company.registration.completed',jsonb_build_object('organizationId',@organizationId,'userId',@userId,'email',@AdministratorEmail),@IdempotencyKey); UPDATE idempotency_keys SET organization_id=@organizationId,response_body=jsonb_build_object('organizationId',@organizationId) WHERE key=@IdempotencyKey", new { organizationId, userId, c.ConsentIpHash, c.AdministratorEmail, c.IdempotencyKey }, uow.Transaction);
        return new(organizationId, legalEntityId, unitId, userId, false);
    }

    private static async Task<RegisterCompanyResult> LoadAsync(IUnitOfWork uow, Guid organizationId, bool replayed) =>
        await uow.Connection.QuerySingleAsync<RegisterCompanyResult>("SELECT o.id OrganizationId,le.id LegalEntityId,u.id UnitId,usr.id UserId,@replayed Replayed FROM organizations o JOIN legal_entities le ON le.organization_id=o.id JOIN units u ON u.legal_entity_id=le.id JOIN users usr ON usr.organization_id=o.id WHERE o.id=@organizationId", new { organizationId, replayed }, uow.Transaction);
}
