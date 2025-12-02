using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            IUserService userService,
            IEmailSender emailSender,
            ILogger<ForgotPasswordModel> logger)
        {
            _userService = userService;
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty]
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        public bool Success { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Generate password reset token (simplified - in production use proper token generation)
                var resetToken = Guid.NewGuid().ToString("N");
                var resetLink = $"{Request.Scheme}://{Request.Host}/ResetPassword?token={resetToken}&email={Uri.EscapeDataString(Email)}";

                // Send email
                await _emailSender.SendEmailAsync(
                    Email,
                    "Đặt lại mật khẩu - Săn Sale Tốt",
                    $@"
                    <h2>Đặt lại mật khẩu</h2>
                    <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản của mình.</p>
                    <p>Nhấn vào link bên dưới để đặt lại mật khẩu:</p>
                    <p><a href='{resetLink}'>Đặt lại mật khẩu</a></p>
                    <p>Link này sẽ hết hạn sau 24 giờ.</p>
                    <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                    <br>
                    <p>Trân trọng,<br>Đội ngũ Săn Sale Tốt</p>
                    ");

                Success = true;
                _logger.LogInformation("Password reset email sent to {Email}", Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", Email);
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra. Vui lòng thử lại sau.");
            }

            return Page();
        }
    }
}
