using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ICartSessionService _cartSessionService;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cartService, ICartSessionService cartSessionService, ILogger<CartController> logger)
    {
        _cartService = cartService;
        _cartSessionService = cartSessionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
    {
        var context = ResolveContext(createAnonymous: true);
        var cart = await _cartService.GetCartAsync(context.UserId, context.AnonymousId, cancellationToken);
        return Ok(cart);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var context = ResolveContext(createAnonymous: true);
        var cart = await _cartService.AddItemAsync(request, context.UserId, context.AnonymousId, cancellationToken);
        return Ok(cart);
    }

    [HttpPatch("items/{productId:int}")]
    public async Task<IActionResult> UpdateItem(int productId, [FromBody] UpdateCartItemRequest request, [FromQuery] int? platformId, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var context = ResolveContext(createAnonymous: false);
        var cart = await _cartService.UpdateQuantityAsync(productId, platformId, request.Quantity, context.UserId, context.AnonymousId, cancellationToken);
        return Ok(cart);
    }

    [HttpDelete("items/{productId:int}")]
    public async Task<IActionResult> RemoveItem(int productId, [FromQuery] int? platformId, CancellationToken cancellationToken = default)
    {
        var context = ResolveContext(createAnonymous: false);
        var cart = await _cartService.RemoveItemAsync(productId, platformId, context.UserId, context.AnonymousId, cancellationToken);
        return Ok(cart);
    }

    [Authorize]
    [HttpPost("merge")]
    public async Task<IActionResult> MergeAnonymousCart(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Forbid();
        }

        var anonymousId = _cartSessionService.GetAnonymousId(HttpContext);
        if (anonymousId == null)
        {
            return NoContent();
        }

        await _cartService.MergeAsync(userId.Value, anonymousId.Value, cancellationToken);
        _cartSessionService.ClearAnonymousCookie(HttpContext);
        return NoContent();
    }

    private (int? UserId, Guid? AnonymousId) ResolveContext(bool createAnonymous)
    {
        var userId = GetUserId();
        Guid? anonymousId = null;
        if (userId == null)
        {
            anonymousId = _cartSessionService.GetAnonymousId(HttpContext, createAnonymous);
        }
        return (userId, anonymousId);
    }

    private int? GetUserId()
    {
        var userIdClaim = User?.FindFirst("uid")?.Value;
        if (int.TryParse(userIdClaim, out var parsed))
        {
            return parsed;
        }
        return null;
    }
}

