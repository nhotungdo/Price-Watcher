using Microsoft.AspNetCore.Http;
using PriceWatcher.Dtos;
using PriceWatcher.Models;

namespace PriceWatcher.Services.Interfaces;

public interface IUserService
{
    Task<User> GetOrCreateUserFromGoogleAsync(GoogleUserInfo info, CancellationToken cancellationToken = default);
    Task OnLoginSuccessAsync(User user, HttpRequest request, CancellationToken cancellationToken = default);
}

