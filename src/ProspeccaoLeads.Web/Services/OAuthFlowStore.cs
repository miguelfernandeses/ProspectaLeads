using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace ProspeccaoLeads.Web.Services;

public sealed class OAuthFlowStore
{
    private const string VerifierKey = "prospectai.auth.pkce.verifier";
    private readonly ProtectedSessionStorage _storage;

    public OAuthFlowStore(ProtectedSessionStorage storage)
    {
        _storage = storage;
    }

    public Task SaveVerifierAsync(string verifier)
    {
        return _storage.SetAsync(VerifierKey, verifier).AsTask();
    }

    public async Task<string?> TakeVerifierAsync()
    {
        var result = await _storage.GetAsync<string>(VerifierKey);
        await _storage.DeleteAsync(VerifierKey);
        return result.Success ? result.Value : null;
    }
}
