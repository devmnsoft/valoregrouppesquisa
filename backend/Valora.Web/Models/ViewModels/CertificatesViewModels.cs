namespace Valora.Web.Models.ViewModels;

public sealed record CertificateItemViewModel(
    Guid Id,
    Guid ResponseId,
    string ParticipantName,
    string ParticipantEmailMasked,
    string CompanyName,
    decimal? TotalScore,
    string Level,
    string ValidationCode,
    string? ValidationUrl,
    string Status,
    DateTimeOffset? IssuedAt) {
    public bool IsIssued => string.Equals(Status, "issued", StringComparison.OrdinalIgnoreCase);
    public string StatusBadgeClass => IsIssued ? "bg-success text-white" : "bg-danger text-white";
    public string StatusDisplayName => IsIssued ? "Emitido" : "Revogado";
}

public sealed record CertificatesIndexViewModel(
    IReadOnlyList<CertificateItemViewModel> Certificates,
    int TotalCertificates,
    int IssuedCount,
    int RevokedCount,
    string? Search);
