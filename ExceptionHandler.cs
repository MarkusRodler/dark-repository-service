using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Dark
{
    [ApiController]
    public class ErrorHandler : ControllerBase
    {
        [Route("/error")]
        public IActionResult ErrorLocalDevelopment([FromServices] IWebHostEnvironment webHostEnvironment)
        {
            if (webHostEnvironment.EnvironmentName != "Development") {
                throw new InvalidOperationException("This shouldn't be invoked in non-development environments.");
            }

            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            var context = HttpContext.Features.Get<IExceptionHandlerFeature>();

            System.Console.WriteLine(context.Error);
            if (context.Error is InvalidOperationException) {
                return UnprocessableEntity(context.Error.Message);
                // Problem(
                //     detail: context.Error.StackTrace,
                //     title: context.Error.Message
                // );
            }
            throw context.Error;
        }
    }
}
