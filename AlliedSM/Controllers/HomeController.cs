using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlliedSM.Controllers
{
    public class HomeController : Controller
    {
        // The public Welcome page is permanently removed from the flow now:
        // "/" never renders a view — it always redirects straight on.
        //   - Not logged in  -> Login page
        //   - Already logged in -> their Dashboard
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            return RedirectToAction("Login", "Account");
        }

        [AllowAnonymous]
        public IActionResult Privacy() => View();
    }
}
