using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using ProspeccaoLeads.Application.DTOs.Auth;
using ProspeccaoLeads.Application.Interfaces;

namespace ProspeccaoLeads.Web.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IAuthService _authService;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(IAuthService authService)
    {
        _authService = authService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = await _authService.GetCurrentUserAsync();
        if (session == null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new Claim(ClaimTypes.Name, session.Name),
            new Claim(ClaimTypes.Email, session.Email),
            new Claim("AccessToken", session.AccessToken ?? "")
        }, "CustomAuth");

        _currentUser = new ClaimsPrincipal(identity);
        return new AuthenticationState(_currentUser);
    }

    public void NotifyUserAuthentication(UserSessionDto session)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new Claim(ClaimTypes.Name, session.Name),
            new Claim(ClaimTypes.Email, session.Email),
            new Claim("AccessToken", session.AccessToken ?? "")
        }, "CustomAuth");

        _currentUser = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public void NotifyUserLogout()
    {
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }
}
