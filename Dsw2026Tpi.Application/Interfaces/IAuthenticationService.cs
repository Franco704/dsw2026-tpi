using Dsw2026Tpi.Application.Dtos;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAuthenticationService
{
    Task<RegisterModel.Response> RegisterAsync(RegisterModel.Request request);
    Task<LoginAdminModel.Response> LoginAdminAsync(LoginAdminModel.Request request);
    Task<LoginPatientModel.Response> LoginPatientAsync(LoginPatientModel.Request request);
}
