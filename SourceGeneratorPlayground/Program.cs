using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapPost("/posts", (Post p) => Results.Ok(p));

app.UseMiddleware<MyMiddleware>();

app.Run();

public class MyOptions
{

}

struct Post
{
    [Required]
    public string Title { get; set; }
}

partial class MyMiddleware
{
    private readonly RequestDelegate _next;

    public MyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context, IOptions<MyOptions> options)
    {
        return _next(context);
    }
}
