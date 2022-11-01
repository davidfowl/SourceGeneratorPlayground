//using System.Diagnostics.CodeAnalysis;
//using Microsoft.AspNetCore.SignalR;
//using Microsoft.AspNetCore.SignalR.Protocol;

//public static class Extensions
//{
//    public static HubEndpointConventionBuilder MapHub<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] THub>(this IEndpointRouteBuilder endpoints, string pattern) where THub : Hub, IBindableHub<THub>
//    {
//        return default;
//    }
//}

//public interface IBindableHub<THub> where THub : Hub
//{
//    static abstract void Bind(IDictionary<string, Func<Hub, HubConnectionContext, HubMessage, CancellationToken, Task>> definition);
//}

//class HubBase<THub> : Hub, IBindableHub<THub> where THub : Hub
//{
//    static void IBindableHub<THub>.Bind(IDictionary<string, Func<Hub, HubConnectionContext, HubMessage, CancellationToken, Task>> definition)
//    {
//        throw new NotImplementedException();
//    }
//}
