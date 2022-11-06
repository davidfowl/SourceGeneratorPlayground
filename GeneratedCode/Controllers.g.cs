using Microsoft.AspNetCore.Http;
public partial class HomeController
{
    public static void BindHub(IDictionary<string, Func<HomeController, Microsoft.AspNetCore.SignalR.HubConnectionContext, Microsoft.AspNetCore.SignalR.Protocol.HubMessage, System.Threading.CancellationToken, Task>> definition)
    {
    }
}
