using ProspeccaoLeads.Application.Common;
using ProspeccaoLeads.Application.DTOs.Auth;

namespace ProspeccaoLeads.Application.Interfaces;

public interface IUserSessionStore
{
    Task<UserSessionDto?> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(UserSessionDto session, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}

public interface IAuthenticationService
{
    Task<Result<UserSessionDto>> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<Result<UserSessionDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<UserSessionDto?> GetCurrentUserAsync(CancellationToken ct = default);
}

public interface IPasswordManagementService
{
    Task<Result> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(ChangePasswordDto dto, CancellationToken ct = default);
    Task<Result> UpdatePasswordWithTokenAsync(string accessToken, string newPassword, CancellationToken ct = default);
}

public interface IUserProfileService
{
    Task<Result<UserSessionDto>> UpdateProfileAsync(UpdateProfileDto dto, CancellationToken ct = default);
}

public interface IAuthService : IAuthenticationService, IPasswordManagementService, IUserProfileService
{
}
