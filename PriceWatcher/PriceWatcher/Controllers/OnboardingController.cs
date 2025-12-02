using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Services.Interfaces;
using System.Security.Claims;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(IOnboardingService onboardingService, ILogger<OnboardingController> logger)
    {
        _onboardingService = onboardingService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var status = await _onboardingService.GetStatusAsync(userId, cancellationToken);
        return Ok(new { status });
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        await _onboardingService.SetStatusAsync(userId, "InProgress", cancellationToken);
        return Ok(new { message = "Onboarding started" });
    }

    [HttpPost("basic-settings")]
    public async Task<IActionResult> SaveBasicSettings([FromBody] BasicSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        await _onboardingService.SaveBasicSettingsAsync(userId, request.Currency, request.Language, cancellationToken);
        return Ok(new { message = "Basic settings saved" });
    }

    [HttpPost("wallet")]
    public async Task<IActionResult> SaveWallet([FromBody] WalletRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        await _onboardingService.SaveFirstWalletAsync(userId, request.Name, request.Type, request.InitialBalance, cancellationToken);
        return Ok(new { message = "Wallet saved" });
    }

    [HttpPost("categories")]
    public async Task<IActionResult> SaveCategories([FromBody] CategoriesRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        await _onboardingService.SaveCategoriesAsync(userId, request.Categories, cancellationToken);
        return Ok(new { message = "Categories saved" });
    }

    [HttpPost("saving-goal")]
    public async Task<IActionResult> SaveSavingGoal([FromBody] SavingGoalRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (monthlyAmount, months) = await _onboardingService.SaveSavingGoalAsync(
            userId, request.GoalName, request.TargetAmount, request.Deadline, cancellationToken);

        return Ok(new { monthlyAmount, months, message = "Saving goal created" });
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        await _onboardingService.CompleteAsync(userId, cancellationToken);
        return Ok(new { message = "Onboarding completed", redirectUrl = "/dashboard" });
    }
}

public record BasicSettingsRequest(string Currency, string Language);
public record WalletRequest(string Name, string Type, decimal InitialBalance);
public record CategoriesRequest(List<string> Categories);
public record SavingGoalRequest(string GoalName, decimal TargetAmount, DateTime Deadline);
