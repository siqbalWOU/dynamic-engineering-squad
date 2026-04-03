using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfrastructureApp_Tests.Account;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options,logger,encoder){ }
    
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Context.Request.Headers.TryGetValue("TestRole", out var role);
        
        var claims = new List<Claim> 
        { 
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.NameIdentifier, "1") 
        };
        
        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
            
        // generating success ticket.
        var ticket = new AuthenticationTicket(principal, "TestScheme");
        var result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}