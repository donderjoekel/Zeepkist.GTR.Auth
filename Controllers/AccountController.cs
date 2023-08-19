using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace TNRD.Zeepkist.GTR.Auth.Controllers;

[ApiController]
[Route("Account")]
public class AccountController : ControllerBase
{
    [HttpGet("AccessDenied")]
    public IActionResult AccessDenied()
    {
        (IHeaderDictionary Headers, IRequestCookieCollection Cookies) data = (Request.Headers, Request.Cookies);

        string json = JsonConvert.SerializeObject(data,
            Formatting.Indented,
            new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
        return Ok(json);
    }
}
