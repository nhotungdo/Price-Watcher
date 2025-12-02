using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Pages
{
    public class ContactModel : PageModel
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ContactModel> _logger;

        public ContactModel(IEmailSender emailSender, ILogger<ContactModel> logger)
        {
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty]
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string? Phone { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Chủ đề là bắt buộc")]
        public string Subject { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Nội dung là bắt buộc")]
        [MinLength(10, ErrorMessage = "Nội dung phải có ít nhất 10 ký tự")]
        public string Message { get; set; } = string.Empty;

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
                var subjectText = Subject switch
                {
                    "general" => "Câu hỏi chung",
                    "support" => "Hỗ trợ kỹ thuật",
                    "feedback" => "Góp ý",
                    "partnership" => "Hợp tác",
                    _ => "Khác"
                };

                // Send notification email to admin
                await _emailSender.SendEmailAsync(
                    "support@sansaletot.vn",
                    $"[Liên hệ] {subjectText} - {Name}",
                    $@"
                    <h3>Liên hệ mới từ website</h3>
                    <p><strong>Họ tên:</strong> {Name}</p>
                    <p><strong>Email:</strong> {Email}</p>
                    <p><strong>Số điện thoại:</strong> {Phone ?? "Không cung cấp"}</p>
                    <p><strong>Chủ đề:</strong> {subjectText}</p>
                    <p><strong>Nội dung:</strong></p>
                    <p>{Message.Replace("\n", "<br>")}</p>
                    ");

                // Send confirmation email to user
                await _emailSender.SendEmailAsync(
                    Email,
                    "Xác nhận liên hệ - Săn Sale Tốt",
                    $@"
                    <h2>Cảm ơn bạn đã liên hệ!</h2>
                    <p>Xin chào {Name},</p>
                    <p>Chúng tôi đã nhận được tin nhắn của bạn về <strong>{subjectText}</strong>.</p>
                    <p>Đội ngũ hỗ trợ của chúng tôi sẽ phản hồi trong thời gian sớm nhất.</p>
                    <br>
                    <p>Trân trọng,<br>Đội ngũ Săn Sale Tốt</p>
                    ");

                Success = true;
                _logger.LogInformation("Contact form submitted by {Name} ({Email})", Name, Email);

                // Clear form
                ModelState.Clear();
                Name = string.Empty;
                Email = string.Empty;
                Phone = null;
                Subject = string.Empty;
                Message = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing contact form");
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra. Vui lòng thử lại sau.");
            }

            return Page();
        }
    }
}
