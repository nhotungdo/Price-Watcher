using Microsoft.AspNetCore.Http;
using PriceWatcher.Dtos;
using PriceWatcher.Models;

namespace PriceWatcher.Services.Interfaces;

public interface IUserService
{
    Task<User> GetOrCreateUserFromGoogleAsync(GoogleUserInfo info, CancellationToken cancellationToken = default);
    Task OnLoginSuccessAsync(User user, HttpRequest request, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User> RegisterLocalAsync(string email, string? fullName, CancellationToken cancellationToken = default);
    Task UpdateLastLoginAsync(User user, CancellationToken cancellationToken = default);
    Task<User> RegisterLocalWithPasswordAsync(string email, string? fullName, string password, CancellationToken cancellationToken = default);
    Task<User?> VerifyLocalLoginAsync(string email, string password, CancellationToken cancellationToken = default);
}

