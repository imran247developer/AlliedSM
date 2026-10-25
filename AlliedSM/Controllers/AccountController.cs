using System.Text;
using AlliedSM.Data;
using AlliedSM.Models;
using AlliedSM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _sms;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager,
            ApplicationDbContext context, ISmsService sms)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _sms = sms;
        }

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email ya password ghalat hai.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account lock hai. Admin se rabta karein.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Email ya password ghalat hai.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

        // ===================== PASSWORD RECOVERY =====================

        [AllowAnonymous]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        // Accepts email, mobile number, or a Student/Staff ID card number
        // (CNIC / B-Form / registration no) and finds the matching login.
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var identifier = model.Identifier.Trim();
            var user = await FindUserByAnyIdentifierAsync(identifier);

            // Always show the same confirmation, whether or not a match was found —
            // this stops someone from using the form to discover which emails/CNICs exist.
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var resetLink = Url.Action("ResetPassword", "Account",
                    new { userId = user.Id, token = encodedToken }, Request.Scheme);

                // Send via SMS if we have a mobile number on file (uses the same
                // ISmsService stub as the rest of the app — swap in a real
                // provider later and this starts actually delivering).
                if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    await _sms.SendAsync(user.PhoneNumber,
                        $"Allied School — password reset link: {resetLink}");
                }

                // No live email/SMS provider is connected yet, so — for now only —
                // we also hand the link straight to the reset page so the flow is
                // fully usable during development. Once a real provider is wired
                // up, delete this ViewBag line and rely on the SMS/email instead.
                ViewBag.DevResetLink = resetLink;
            }

            return View("ForgotPasswordConfirmation");
        }

        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(Login));

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            return View(new ResetPasswordViewModel { UserId = userId, Token = decodedToken });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                // Same "generic success" principle as ForgotPassword — don't reveal whether the id existed.
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (result.Succeeded)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation() => View();

        private async Task<ApplicationUser?> FindUserByAnyIdentifierAsync(string identifier)
        {
            // 1) Try as an email / username
            var user = await _userManager.FindByEmailAsync(identifier);
            if (user != null) return user;

            // 2) Try as a mobile number (PhoneNumber on the login account)
            user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == identifier);
            if (user != null) return user;

            // 3) Try as a Student's ID card / B-Form no. or registration no. (Parent logins)
            var student = await _context.Students.FirstOrDefaultAsync(s =>
                s.StudentCnicOrBForm == identifier || s.RegistrationNo == identifier);
            if (student != null)
            {
                user = await _userManager.Users.FirstOrDefaultAsync(u => u.LinkedStudentId == student.StudentId);
                if (user != null) return user;
            }

            // 4) Try as a Staff CNIC (Teacher/Accountant logins)
            var staff = await _context.Staff.FirstOrDefaultAsync(s => s.Cnic == identifier);
            if (staff != null)
            {
                user = await _userManager.Users.FirstOrDefaultAsync(u => u.LinkedStaffId == staff.StaffId);
                if (user != null) return user;
            }

            return null;
        }
    }
}
