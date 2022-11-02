using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapPost("/posts", (Post p) => Results.Ok(p));


app.Run();

struct Post
{
    [Required]
    public string Title { get; set; }
}