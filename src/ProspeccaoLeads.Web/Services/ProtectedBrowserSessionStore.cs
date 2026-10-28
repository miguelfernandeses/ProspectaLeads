using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using ProspeccaoLeads.Application.DTOs.Auth;
using ProspeccaoLeads.Application.Interfaces;

namespace ProspeccaoLeads.Web.Services;

public sealed class ProtectedBrowserSessionStore : IUserSessionStore
{
    private const string SessionKey = "prospectai.auth.session";
    private readonly ProtectedLocalStorage _storage;
    private readonly ILogger<ProtectedBrowserSessionStore> _logger;

    public ProtectedBrowserSessionStore(
        ProtectedLocalStorage storage,
        ILogger<ProtectedBrowserSessionStore> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<UserSessionDto?> LoadAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var result = await _storage.GetAsync<UserSessionDto>(SessionKey);
            return result.Success ? result.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível restaurar a sessão protegida.");
            return null;
        }
    }

    public async Task SaveAsync(UserSessionDto session, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await _storage.SetAsync(SessionKey, session);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await _storage.DeleteAsync(SessionKey);
    }
}
