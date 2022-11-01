using Microsoft.AspNetCore.Mvc;

//namespace SourceGeneratorPlayground;

public partial class HomeController : ControllerBase
{
    [HttpGet("/")]
    public string Get() => "Hello World";
}