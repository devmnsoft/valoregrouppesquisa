namespace Valora.Application.CommercialDelivery;

public sealed record PublicLeadRequest(string Name, string Email, string? Phone, string CompanyName,
    string Segment, string CompanySize, string? RoleTitle, bool ConsentAccepted, bool CommunicationConsent,
    string Source = "public_free_diagnostic");
public sealed record PublicLeadCreated(Guid LeadId, Guid SessionId, string Status);
public sealed record CommercialRequestInput(Guid LeadId, Guid? SessionId, string RequestType, string? RequestedPlan, string? Notes);
public sealed record CommercialLeadItem(Guid Id, string Name, string EmailMasked, string? PhoneMasked,
    string CompanyName, string Segment, string CompanySize, string Status, string? LastResultLevel,
    decimal? LastResultScore, string? PlanInterest, Guid? AssignedTo, DateTime CreatedAt);
public sealed record CommercialDashboard(int NewLeads, int DiagnosticsStarted, int DiagnosticsCompleted,
    int ContactRequests, int ProfessionalRequests, int EnterpriseRequests, int Converted, int Lost);

public interface IPublicCommercialRepository
{
    Task<PublicLeadCreated> UpsertAndStartAsync(PublicLeadRequest request, string emailHash, string emailMasked,
        string? phoneHash, string? phoneMasked, string? ipHash, string? userAgentHash, CancellationToken ct);
    Task<Guid> CreateRequestAsync(CommercialRequestInput request, CancellationToken ct);
    Task<IReadOnlyList<CommercialLeadItem>> ListAsync(string? status, string? level, string? plan, int page, int pageSize, CancellationToken ct);
    Task<CommercialDashboard> DashboardAsync(CancellationToken ct);
    Task<bool> UpdateStatusAsync(Guid id, string status, Guid? assignedTo, string? reason, CancellationToken ct);
}
