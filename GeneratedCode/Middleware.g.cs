partial class MyMiddleware
{
    public static Microsoft.AspNetCore.Http.RequestDelegate CreateDelegate(Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Http.RequestDelegate next)
    {
        MyMiddleware m = new MyMiddleware(next);
        Task HandleRequest(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var options = context.RequestServices.GetService<Microsoft.Extensions.Options.IOptions<MyOptions>>();
            return m.Invoke(context, options);
        }
        return HandleRequest;
    }
}
