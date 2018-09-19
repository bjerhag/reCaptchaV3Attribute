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
    public class ReCaptchaFilterAttribute : ActionFilterAttribute
    {
        internal IConfiguration Config { get; set; }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            Console.WriteLine(context?.ToString() == "");
            try
            {
                if (context == null)
                {
                    return;
                }
                var secret = Config.GetSection("Recaptcha:Secret")?.Value ?? "";
                float.TryParse(Config.GetSection("Recaptcha:ScoreLimit")?.Value ?? "0", out var scoreLimit);
                if (string.IsNullOrEmpty(secret))
                {
                    ((ControllerBase)context.Controller).ModelState.AddModelError("ReCaptcha", "Error");
                    context.Result = ((ControllerBase)context.Controller).BadRequest("ReCaptcha Error");
                    return;
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
                    else if (result.Score != null && float.TryParse(result.Score, out var score) && score < scoreLimit)
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
