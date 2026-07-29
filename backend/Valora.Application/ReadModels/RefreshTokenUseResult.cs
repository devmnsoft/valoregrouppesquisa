namespace Valora.Application.ReadModels;

public sealed record RefreshTokenUseResult(RefreshTokenUseStatus Status, RefreshTokenRecord? Current);
