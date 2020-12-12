using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CodeIndex.Common;
using CodeIndex.MaintainIndex;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CodeIndex.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ManagementController : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost]
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

        public async Task<FetchResult<IndexStatusInfo[]>> GetIndexLists([FromServices] IndexManagement indexManagement)
        {
            return await Task.FromResult(indexManagement.GetIndexList());
        }

        [HttpPost]
        public async Task<FetchResult<bool>> AddIndex(IndexConfig indexConfig, [FromServices] IndexManagement indexManagement)
        {
            return await Task.FromResult(indexManagement.AddIndex(indexConfig));
        }

        [HttpPost]
        public async Task<FetchResult<bool>> EditIndex(IndexConfig indexConfig, [FromServices] IndexManagement indexManagement)
        {
            return await Task.FromResult(indexManagement.EditIndex(indexConfig));
        }

        [HttpPost]
        public async Task<FetchResult<bool>> DeleteIndex(string indexName, [FromServices] IndexManagement indexManagement)
        {
            return await Task.FromResult(indexManagement.DeleteIndex(indexName));
        }

        [HttpPost]
        public async Task<FetchResult<bool>> StopIndex(string indexName, [FromServices] IndexManagement indexManagement)
        {
            return await Task.FromResult(indexManagement.StopIndex(indexName));
        }
    }
}
