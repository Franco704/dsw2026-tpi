using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace Dsw2026Tpi.Api.Services;

public class SignInService(SignInManager<ApplicationUser> signInManager) : ISignInService
{
    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
    {
        var result = await signInManager.CheckPasswordSignInAsync(user, password, false);
        return result.Succeeded;
    }
}
