using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CodeIndex.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CodeIndex.Server.Controllers
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    public class ManagementController : ControllerBase
    {
        [AllowAnonymous]
        public async Task<ClientLoginModel> Login([FromServices] CodeIndexConfiguration codeIndexConfiguration, ClientLoginModel loginModel)
        {
            if (string.IsNullOrWhiteSpace(loginModel.Captcha) || HttpContext.Session.GetString(nameof(ClientLoginModel.Captcha)) != loginModel.Captcha.ToLowerInvariant())
            {
                loginModel.Status = LoginStatus.Failed;
                loginModel.Message = "Wrong captcha";
            }
            else
            {
                var user = codeIndexConfiguration.ManagerUsers?.FirstOrDefault(u => u.UserName == loginModel.UserName && u.Password == loginModel.Password);

                if (user == null)
                {
                    loginModel.Status = LoginStatus.Failed;
                    loginModel.Message = "Wrong username or password";
                }
                else
                {
                    loginModel.Status = LoginStatus.Succesful;

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.UserName)
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    var properties = new AuthenticationProperties();

                    if (loginModel.Persist)
                    {
                        properties.IsPersistent = true;
                        properties.ExpiresUtc = DateTime.UtcNow.AddMonths(12);
                    }

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
                }
            }

            HttpContext.Session.Remove(nameof(ClientLoginModel.Captcha));
            return loginModel;
        }

        public async Task Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        [AllowAnonymous]
        public IActionResult GenerateCaptcha()
        {
            HttpContext.Session.SetString(nameof(ClientLoginModel.Captcha), CaptchaUtils.GenerateCaptcha(300, 60, out var images).ToLowerInvariant());

            return File(images, "image/png");
        }

        public async Task GetIndexLists()
        {
            await Task.FromException(new NotImplementedException());
        }

        public async Task AddIndex(IndexConfig indexConfig)
        {
            await Task.FromException(new NotImplementedException());
        }

        public async Task EditIndex(IndexConfig indexConfig)
        {
            await Task.FromException(new NotImplementedException());
        }

        public async Task DeleteIndex(Guid indexConfigPK)
        {
            await Task.FromException(new NotImplementedException());
        }

        public async Task StopIndex(Guid indexConfigPK)
        {
            await Task.FromException(new NotImplementedException());
        }
    }
}
