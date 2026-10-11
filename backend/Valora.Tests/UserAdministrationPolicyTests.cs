using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Exceptions;
using Valora.Application.Services;

namespace Valora.Tests;

public sealed class UserAdministrationPolicyTests {
    [Fact]
    public async Task TenantAdministratorCannotCreateUserWithPlatformRole() {
        var repository = new RecordingUserAdministrationRepository();
        var service = new UserAdministrationService(repository);

        await Assert.ThrowsAsync<ForbiddenAppException>(() => service.CreateAdministrativeAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            actorIsGlobalAdministrator: false,
            new CreateAdministrativeUserRequest("Usuário", "user@example.test", "hash", "admin_valora")));

        Assert.Equal(0, repository.CreateCalls);
    }

    [Fact]
    public async Task TenantAdministratorCannotPartiallyUpdateWhenRoleIsForbidden() {
        var repository = new RecordingUserAdministrationRepository();
        var service = new UserAdministrationService(repository);

        await Assert.ThrowsAsync<ForbiddenAppException>(() => service.UpdateAdministrativeAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            actorIsGlobalAdministrator: false,
            Guid.NewGuid(),
            new UpdateAdministrativeUserRequest("Usuário", "11999999999", "active", ["empresa_admin", "admin_valora"])));

        Assert.Equal(0, repository.UpdateAdministrativeCalls);
    }

    [Fact]
    public async Task GlobalAdministratorCanGrantPlatformRoleThroughAdministrativeUseCase() {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repository = new RecordingUserAdministrationRepository {
            Created = new UserAdministrationResponse(userId, "Usuário", "user@example.test", null, "active", ["admin_valora"], [], null, DateTimeOffset.UtcNow, null)
        };
        var service = new UserAdministrationService(repository);

        var created = await service.CreateAdministrativeAsync(
            organizationId,
            Guid.NewGuid(),
            actorIsGlobalAdministrator: true,
            new CreateAdministrativeUserRequest(" Usuário ", "USER@EXAMPLE.TEST ", "hash", "admin_valora"));

        Assert.Equal(userId, created.Id);
        Assert.Equal(1, repository.CreateCalls);
        Assert.Equal("user@example.test", repository.LastCreateRequest?.Email);
    }

    private sealed class RecordingUserAdministrationRepository : IUserAdministrationRepository {
        public int CreateCalls { get; private set; }
        public int UpdateAdministrativeCalls { get; private set; }
        public CreateAdministrativeUserRequest? LastCreateRequest { get; private set; }
        public UserAdministrationResponse? Created { get; set; }

        public Task<(IReadOnlyList<UserAdministrationResponse> Items, long Total)> ListAsync(Guid organizationId, UserListQuery query, CancellationToken ct = default) =>
            Task.FromResult<(IReadOnlyList<UserAdministrationResponse>, long)>(([], 0));

        public Task<UserAdministrationResponse?> GetAsync(Guid organizationId, Guid userId, CancellationToken ct = default) =>
            Task.FromResult<UserAdministrationResponse?>(null);

        public Task<UserAdministrationResponse?> CreateAdministrativeAsync(Guid organizationId, Guid actorId, CreateAdministrativeUserRequest request, CancellationToken ct = default) {
            CreateCalls++;
            LastCreateRequest = request;
            return Task.FromResult(Created);
        }

        public Task<bool> UpdateAsync(Guid organizationId, Guid userId, UpdateUserRequest request, CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task<bool> UpdateStatusAsync(Guid organizationId, Guid userId, string status, CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task<UserAdministrationResponse?> UpdateAdministrativeAsync(Guid organizationId, Guid actorId, Guid userId, UpdateAdministrativeUserRequest request, CancellationToken ct = default) {
            UpdateAdministrativeCalls++;
            return Task.FromResult<UserAdministrationResponse?>(null);
        }

        public Task<int> CountAdministratorsAsync(Guid organizationId, CancellationToken ct = default) => Task.FromResult(1);
        public Task<bool> IsAdministratorAsync(Guid organizationId, Guid userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task ReplaceRolesAsync(Guid organizationId, Guid userId, IReadOnlyList<string> roleCodes, CancellationToken ct = default) => Task.CompletedTask;
        public Task ReplaceScopesAsync(Guid organizationId, Guid userId, IReadOnlyList<UserScopeRequest> scopes, CancellationToken ct = default) => Task.CompletedTask;
    }
}
