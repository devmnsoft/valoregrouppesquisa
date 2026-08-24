using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

public sealed class OrganizationBrandingRepository(IDbConnectionFactory factory, IOrganizationRepository organizations) : IOrganizationBrandingRepository
{
    public async Task<OrganizationBrandingResponse> GetAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        using var c=factory.Create();
        const string sql="""SELECT COALESCE(primary_color,'#0B3D4D') PrimaryColor,COALESCE(secondary_color,'#D7A94B') SecondaryColor,logo_url LogoUrl,COALESCE(public_slug,'') PublicSlug,white_label_enabled WhiteLabelEnabled,version Version FROM valorapesquisa.organization_branding WHERE organization_id=@organizationId""";
        return await c.QuerySingleOrDefaultAsync<OrganizationBrandingResponse>(new CommandDefinition(sql,new{organizationId},cancellationToken:cancellationToken)) ?? new("#0B3D4D","#D7A94B",null,"",false,1);
    }
    public async Task<bool> HasCapabilityAsync(Guid organizationId,string capability,CancellationToken cancellationToken=default)
    {
        using var c=factory.Create(); const string sql="""SELECT COALESCE(bool_or(pc.enabled),false) FROM valorapesquisa.subscriptions s JOIN valorapesquisa.plan_capabilities pc ON pc.plan_id=s.plan_id WHERE s.organization_id=@organizationId AND s.status IN ('active','trialing') AND (s.status<>'trialing' OR COALESCE(s.trial_ends_at,s.ends_at)>now()) AND s.deleted_at IS NULL AND lower(pc.capability_key)=lower(@capability)""";
        return await c.ExecuteScalarAsync<bool>(new CommandDefinition(sql,new{organizationId,capability},cancellationToken:cancellationToken));
    }
    public async Task<OrganizationBrandingResponse?> UpdateAsync(Guid organizationId,UpdateOrganizationBrandingRequest request,CancellationToken cancellationToken=default)
    {
        using var c=factory.Create(); const string sql="""
        INSERT INTO valorapesquisa.organization_branding(organization_id,primary_color,secondary_color,logo_url,public_slug,white_label_enabled,version,updated_at)
        VALUES(@organizationId,@PrimaryColor,@SecondaryColor,@LogoUrl,@PublicSlug,@WhiteLabelEnabled,2,now())
        ON CONFLICT(organization_id) DO UPDATE SET primary_color=EXCLUDED.primary_color,secondary_color=EXCLUDED.secondary_color,logo_url=EXCLUDED.logo_url,public_slug=EXCLUDED.public_slug,white_label_enabled=EXCLUDED.white_label_enabled,version=organization_branding.version+1,updated_at=now()
        WHERE organization_branding.version=@Version
        RETURNING primary_color PrimaryColor,secondary_color SecondaryColor,logo_url LogoUrl,public_slug PublicSlug,white_label_enabled WhiteLabelEnabled,version Version
        """;
        try{return await c.QuerySingleOrDefaultAsync<OrganizationBrandingResponse>(new CommandDefinition(sql,new{organizationId,request.PrimaryColor,request.SecondaryColor,request.LogoUrl,request.PublicSlug,request.WhiteLabelEnabled,request.Version},cancellationToken:cancellationToken));}catch(Npgsql.PostgresException e) when(e.SqlState=="23505"){return null;}
    }
    public async Task<OrganizationSubscriptionResponse?> GetSubscriptionAsync(Guid organizationId,CancellationToken cancellationToken=default)
    {
        using var c=factory.Create(); const string head="""SELECT s.id SubscriptionId,p.code PlanCode,p.name PlanName,s.status Status,s.starts_at StartsAt,s.ends_at EndsAt FROM valorapesquisa.subscriptions s JOIN valorapesquisa.plans p ON p.id=s.plan_id WHERE s.organization_id=@organizationId AND s.deleted_at IS NULL ORDER BY (s.status='active') DESC,s.starts_at DESC LIMIT 1""";
        var subscription=await c.QuerySingleOrDefaultAsync<SubscriptionRow>(new CommandDefinition(head,new{organizationId},cancellationToken:cancellationToken)); if(subscription is null)return null;
        var capabilities=(await c.QueryAsync<string>(new CommandDefinition("SELECT pc.capability_key FROM valorapesquisa.plan_capabilities pc JOIN valorapesquisa.subscriptions s ON s.plan_id=pc.plan_id WHERE s.id=@id AND pc.enabled ORDER BY pc.capability_key",new{id=subscription.SubscriptionId},cancellationToken:cancellationToken))).AsList();
        var limits=(await c.QueryAsync<LimitRow>(new CommandDefinition("SELECT pl.limit_key Key,pl.limit_value Value FROM valorapesquisa.plan_limits pl JOIN valorapesquisa.subscriptions s ON s.plan_id=pl.plan_id WHERE s.id=@id",new{id=subscription.SubscriptionId},cancellationToken:cancellationToken))).ToDictionary(x=>x.Key,x=>(long?)x.Value);
        var usage=await organizations.GetUsageAsync(organizationId); var metrics=usage.Select(x=>new OrganizationMetricResponse(x.Key,Label(x.Key),x.Period,x.Consumed,x.Reserved,x.Limit,x.Available,x.Percentage,x.Unlimited)).ToList();
        return new(subscription.SubscriptionId,subscription.PlanCode,subscription.PlanName,subscription.Status,subscription.StartsAt,subscription.EndsAt,capabilities,limits,metrics);
    }
    public async Task<IReadOnlyList<OnboardingStepResponse>> GetOnboardingAsync(Guid organizationId,CancellationToken cancellationToken=default)
    {
        using var c=factory.Create(); const string sql="""
        WITH facts AS (SELECT
          EXISTS(SELECT 1 FROM valorapesquisa.organizations WHERE id=@organizationId AND public_name IS NOT NULL AND email IS NOT NULL AND phone IS NOT NULL) company_profile,
          EXISTS(SELECT 1 FROM valorapesquisa.organization_branding WHERE organization_id=@organizationId AND public_slug IS NOT NULL) branding,
          EXISTS(SELECT 1 FROM valorapesquisa.legal_entities WHERE organization_id=@organizationId AND status='active' AND deleted_at IS NULL) main_legal_entity,
          EXISTS(SELECT 1 FROM valorapesquisa.units WHERE organization_id=@organizationId AND status='active' AND deleted_at IS NULL) first_unit,
          (EXISTS(SELECT 1 FROM valorapesquisa.user_invitations WHERE organization_id=@organizationId AND status IN('pending','accepted')) OR (SELECT count(*)>1 FROM valorapesquisa.users WHERE organization_id=@organizationId AND status='active' AND deleted_at IS NULL)) invite_team,
          EXISTS(SELECT 1 FROM valorapesquisa.surveys WHERE organization_id=@organizationId AND deleted_at IS NULL) first_survey),
        steps(code,label,description,action_url,automatic,permission,done) AS (VALUES
          ('company_profile','Perfil da empresa','Complete os dados essenciais.','/Organization',true,'organization.update',(SELECT company_profile FROM facts)),('branding','Identidade visual','Configure cores, logotipo e slug.','/Organization#org-branding',true,'organization.branding.update',(SELECT branding FROM facts)),('main_legal_entity','Pessoa jurídica principal','Cadastre a primeira pessoa jurídica.','/LegalEntities',true,'legal_entities.create',(SELECT main_legal_entity FROM facts)),('first_unit','Primeira unidade','Cadastre uma unidade operacional.','/Units',true,'units.create',(SELECT first_unit FROM facts)),('invite_team','Convide a equipe','Convide outra pessoa para colaborar.','/Users#invitations-list',true,'invitations.create',(SELECT invite_team FROM facts)),('first_survey','Primeira pesquisa','Crie a primeira pesquisa.','/Surveys',true,'surveys.create',(SELECT first_survey FROM facts)))
        SELECT code Code,label Label,description Description,CASE WHEN done THEN 'completed' ELSE 'pending' END Status,CASE WHEN done THEN COALESCE(os.completed_at,now()) END CompletedAt,action_url ActionUrl,automatic Automatic,permission RequiredPermission FROM steps LEFT JOIN valorapesquisa.onboarding_steps os ON os.organization_id=@organizationId AND os.step_code=steps.code ORDER BY array_position(ARRAY['company_profile','branding','main_legal_entity','first_unit','invite_team','first_survey'],code)
        """;
        return (await c.QueryAsync<OnboardingStepResponse>(new CommandDefinition(sql,new{organizationId},cancellationToken:cancellationToken))).AsList();
    }
    public async Task<bool> CompleteStepAsync(Guid organizationId,string stepCode,CancellationToken cancellationToken=default){using var c=factory.Create();return await c.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.onboarding_steps(organization_id,step_code,status,completed_at) VALUES(@organizationId,@stepCode,'completed',now()) ON CONFLICT(organization_id,step_code) DO UPDATE SET status='completed',completed_at=now(),updated_at=now()",new{organizationId,stepCode},cancellationToken:cancellationToken))>0;}
    private static string Label(string key)=>string.Join(' ',key.Split('_','-').Select(x=>char.ToUpperInvariant(x[0])+x[1..]));
    private sealed record SubscriptionRow(Guid SubscriptionId,string PlanCode,string PlanName,string Status,DateTimeOffset StartsAt,DateTimeOffset? EndsAt);
    private sealed record LimitRow(string Key,int? Value);
}
