using System;
using DDreCaptcha.Attributes;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace DDreCaptcha.FilterFactories
{
    public class ReCaptchaFilterAttributeFactory : IFilterFactory
    {
        public bool IsReusable => false;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var config = serviceProvider.GetService(typeof(IConfiguration)) as IConfiguration;
            var attribute = new ReCaptchaFilterAttribute();
            attribute.Config = config;
            return attribute;

        }
    }
}
