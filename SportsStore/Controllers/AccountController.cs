using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportsStore.Models.ViewModels;

namespace SportsStore.Controllers {

    public class AccountController : Controller {
        private UserManager<IdentityUser> userManager;
        private SignInManager<IdentityUser> signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<IdentityUser> userMgr,
                SignInManager<IdentityUser> signInMgr,
                ILogger<AccountController> logger) {
            userManager = userMgr;
            signInManager = signInMgr;
            _logger = logger;
        }

        public ViewResult Login(string returnUrl) {
            return View(new LoginModel {
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel loginModel) {
            if (ModelState.IsValid) {
                IdentityUser? user =
                    await userManager.FindByNameAsync(loginModel.Name ?? string.Empty);
                if (user != null) {
                    await signInManager.SignOutAsync();
                    if ((await signInManager.PasswordSignInAsync(user,
                            loginModel.Password ?? string.Empty, false, false)).Succeeded) {
                        _logger.LogInformation("User {UserName} logged in successfully", user.UserName);
                        return Redirect(loginModel?.ReturnUrl ?? "/Admin");
                    }
                }
                _logger.LogWarning("Failed login attempt for {UserName}", loginModel.Name);
                ModelState.AddModelError("", "Invalid name or password");
            }
            return View(loginModel);
        }

        [Authorize]
        public async Task<RedirectResult> Logout(string returnUrl = "/") {
            _logger.LogInformation("User {UserName} logged out", User.Identity?.Name);
            await signInManager.SignOutAsync();
            return Redirect(returnUrl);
        }
    }
}
