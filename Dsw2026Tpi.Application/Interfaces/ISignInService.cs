using Dsw2026Tpi.Data.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Interfaces;

public interface ISignInService
{
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
}
