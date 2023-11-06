using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Piranha.AspNetCore.Identity.Data;
using Piranha.Manager.LocalAuth;

namespace CMS
{
    [Route("user")]
    [AllowAnonymous]
    [ApiController]
    public class UserController : Controller
    {
        private readonly ISecurity security;
        private readonly UserManager<User> userManager;

        public UserController(ISecurity security, UserManager<User> userManager)
        {
            this.security = security;
            this.userManager = userManager;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IResult> Login(LoginInput login)
        {
            return await security.SignIn(HttpContext, login.username, login.password) == LoginResult.Succeeded
                ? Results.Redirect("/")
                : Results.Unauthorized();
        }

        [HttpPost]
        [Route("logout")]
        public async Task Logout()
        {
            await security.SignOut(HttpContext);
        }

        [HttpGet]
        [Route("info")]
        public async Task<IResult> GetUserInfo()
        {
            return User.Identity.IsAuthenticated
                ? Results.Ok(await userManager.FindByNameAsync(User.Identity.Name!))
                : Results.Unauthorized();
        }
    }
}