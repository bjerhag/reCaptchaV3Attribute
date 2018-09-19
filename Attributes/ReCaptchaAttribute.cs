using System;
using System.Linq;
using System.Net.Http;
using DDreCaptcha.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace DDreCaptcha.Attributes
{

    /// <summary>
    /// To much checks and crap. cleanup i needed
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ReCaptchaFilterAttribute : ActionFilterAttribute
    {
        internal IConfiguration Config { get; set; }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                if (context == null)
                {
                    return;
                }

                Config = context.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                if(Config == null)
                {
                    ((ControllerBase)context.Controller).ModelState.AddModelError("ReCaptcha", "Error");
                    context.Result = ((ControllerBase)context.Controller).BadRequest("ReCaptcha Not configured");
                }

                var secret = Config.GetSection("Recaptcha:Secret")?.Value ?? "";
                float.TryParse(Config.GetSection("Recaptcha:ScoreLimit")?.Value ?? "0", out var scoreLimit);
                if (string.IsNullOrEmpty(secret))
                {
                    ((ControllerBase)context.Controller).ModelState.AddModelError("ReCaptcha", "Error");
                    context.Result = ((ControllerBase)context.Controller).BadRequest("ReCaptcha Error");
                }

                var httpContext = context.HttpContext;
                var recaptchaResponse = httpContext.Request.Headers["recaptcha-response"].FirstOrDefault();
                var recaptchaAction = httpContext.Request.Headers["recaptcha-action"].FirstOrDefault();
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(@"https://www.google.com/recaptcha/api/siteverify");

                    var response = client.GetStringAsync($"?response={recaptchaResponse}&secret={secret}&action={recaptchaAction}")?.Result;
                    var result = JsonConvert.DeserializeObject<RecaptchaResponse>(response);

                    if (result.Success == false)
                    {
                        ((ControllerBase)context.Controller).ModelState.AddModelError("ReCaptcha", "Error");
                        context.Result =
                            ((ControllerBase)context.Controller).BadRequest(
                                $"ReCaptcha Error, {string.Join(',', result.ErrorCodes)}");
                    }
                    else if (!string.IsNullOrEmpty(result.Score) && float.TryParse(result.Score, out var score) && score < scoreLimit)
                    {
                        ((ControllerBase)context.Controller).ModelState.AddModelError("ReCaptcha", "Error");
                        context.Result =
                            ((ControllerBase)context.Controller).BadRequest(
                                $"Recaptcha score to low ({score})");
                    }
                }
            }
            catch (Exception e)
            {
                if (context == null)
                    return;
                ((ControllerBase)context.Controller).ModelState.AddModelError("ReCaptcha", "Error");
                context.Result = ((ControllerBase)context.Controller).BadRequest($"ReCaptcha Error, {e.Message}, {e.StackTrace}, {e.InnerException?.Message}, {e.InnerException?.StackTrace}");
            }
        }
    }
}
