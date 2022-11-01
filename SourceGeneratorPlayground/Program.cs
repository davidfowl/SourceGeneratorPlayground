using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using Microsoft.AspNetCore.SignalR;

//foreach (var t in typeof(IHubCallerClients<>).GetInterfaces().Concat(new[] { typeof(IHubCallerClients<>) }))
//{
//    Console.WriteLine(t);
//    DumpMethods(t);
//}

DumpMethods(typeof(ChatHub));


static void DumpMethods(Type? t)
{
    foreach (var p in t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
    {
        // Console.WriteLine(p.DeclaringType + " and " + p.ReflectedType);
        Console.WriteLine(p.Name);
        Console.WriteLine(p.GetType().GetProperty("BindingFlags", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(p, null));
    }
}

Console.WriteLine("Done");

class Foo : Bar
{
    // public override int X => base.X;
}

class Bar
{
    public virtual int X { get; }
}
//var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddSignalR();
//var app = builder.Build();

//app.MapGet("/", () => "Hello World!");

//app.MapHub<ChatHub>("/chat");

//app.Run();

