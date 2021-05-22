using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Dark
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try {
                await next(context);
            }
            catch (InvalidOperationException exception) {
                // context.Response.ContentType = "application/json";
                context.Response.StatusCode = 422; // UnprocessableEntity ErrorCode
                await context.Response.WriteAsync(exception.Message);
            }
        }
    }
}
