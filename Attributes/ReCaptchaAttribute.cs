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
                var scoreLimit = float.Parse(Config.GetSection("Recaptcha:ScoreLimit")?.Value ?? "0");
                if (string.IsNullOrEmpty(secret))
                {
                    ((ControllerBase)context.Controller).ModelState.AddModelError("ReCaptcha", "Error");
                    context.Result = ((ControllerBase)context.Controller).BadRequest("ReCaptcha Error");
                    Console.WriteLine("No recaptcha secret");
                    return;
                }

                var httpContext = context.HttpContext;
                var recaptchaResponse = httpContext.Request.Headers["recaptcha_response"].FirstOrDefault();
                var recaptchaAction = httpContext.Request.Headers["recaptcha_action"].FirstOrDefault();
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(@"https://www.google.com/recaptcha/api/siteverify");

                    var response = client.GetStringAsync($"?response={recaptchaResponse}&secret={secret}&action={recaptchaAction}").Result;
                    var result = JsonConvert.DeserializeObject<RecaptchaResponse>(response);

                    if (result.Success == false || result.Score < scoreLimit)
                    {
                        ((ControllerBase)context.Controller).ModelState.AddModelError("ReCaptcha", "Error");
                        context.Result = ((ControllerBase)context.Controller).BadRequest($"ReCaptcha Error, {string.Join(',', result.ErrorCodes)}");
                        Console.WriteLine(string.Join(',', result.ErrorCodes));
                    }
                }
            }
            catch (Exception e)
            {
                ((ControllerBase)context.Controller).ModelState.AddModelError("ReCaptcha", "Error");
                context.Result = ((ControllerBase)context.Controller).BadRequest("ReCaptcha Error");
                Console.WriteLine(e.Message + e.InnerException.StackTrace);
            }
        }
    }
}
