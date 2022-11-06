namespace Microsoft.AspNetCore.SignalR
{
    // These are types that will be in the framework
    public interface IHubDefinition
    {
        void AddHubMethod(string name, HubInvocationDelegate handler);
        void SetHubInitializer(HubInitializerDelegate initializer);
    }
    public interface IStreamTracker
    {
        void AddStream(string name, System.Func<object, ValueTask> writeStreamItem, System.Func<System.Exception, bool> completeStream);
        void RemoveStream(string name);
    }
    public delegate Task HubInvocationDelegate(Microsoft.AspNetCore.SignalR.Hub hub, Microsoft.AspNetCore.SignalR.HubConnectionContext connection, Microsoft.AspNetCore.SignalR.IStreamTracker streamTracker, Microsoft.AspNetCore.SignalR.Protocol.HubMessage message, System.Threading.CancellationToken cancellationToken);
    public delegate void HubInitializerDelegate(Microsoft.AspNetCore.SignalR.Hub hub, Microsoft.AspNetCore.SignalR.HubConnectionContext connection, Microsoft.AspNetCore.SignalR.IHubCallerClients clients);
}
partial class ChatHub
{
    static async Task SendThunk(Microsoft.AspNetCore.SignalR.Hub hub, Microsoft.AspNetCore.SignalR.HubConnectionContext connection, Microsoft.AspNetCore.SignalR.IStreamTracker streamTracker, Microsoft.AspNetCore.SignalR.Protocol.HubMessage message, System.Threading.CancellationToken cancellationToken)
    {
        var invocation = (Microsoft.AspNetCore.SignalR.Protocol.InvocationMessage)message;
        var args = invocation.Arguments;
        try
        {
            await ((ChatHub)hub).Send((string)args[0]);
        }
        catch (Exception ex) when (invocation.InvocationId is not null)
        {
            await connection.WriteAsync(Microsoft.AspNetCore.SignalR.Protocol.CompletionMessage.WithError(invocation.InvocationId, "Invoking Send failed"));
            return;
        }
        finally
        {
        }
        
        if (invocation.InvocationId is not null)
        {
            await connection.WriteAsync(Microsoft.AspNetCore.SignalR.Protocol.CompletionMessage.WithResult(invocation.InvocationId, null));
        }
    }
    
    static async Task LoopThunk(Microsoft.AspNetCore.SignalR.Hub hub, Microsoft.AspNetCore.SignalR.HubConnectionContext connection, Microsoft.AspNetCore.SignalR.IStreamTracker streamTracker, Microsoft.AspNetCore.SignalR.Protocol.HubMessage message, System.Threading.CancellationToken cancellationToken)
    {
        var invocation = (Microsoft.AspNetCore.SignalR.Protocol.InvocationMessage)message;
        var args = invocation.Arguments;
        var streamItemMessage = new Microsoft.AspNetCore.SignalR.Protocol.StreamItemMessage(invocation.InvocationId, null);
        try
        {
            await foreach (var item in ((ChatHub)hub).Loop((int)args[0], cancellationToken).WithCancellation(cancellationToken))
            {
                streamItemMessage.Item = item;
                await connection.WriteAsync(streamItemMessage);
            }
        }
        catch (Exception ex)
        {
        }
        finally
        {
        }
    }
    
    static async Task UploadDataThunk(Microsoft.AspNetCore.SignalR.Hub hub, Microsoft.AspNetCore.SignalR.HubConnectionContext connection, Microsoft.AspNetCore.SignalR.IStreamTracker streamTracker, Microsoft.AspNetCore.SignalR.Protocol.HubMessage message, System.Threading.CancellationToken cancellationToken)
    {
        var invocation = (Microsoft.AspNetCore.SignalR.Protocol.StreamInvocationMessage)message;
        var channel2 = System.Threading.Channels.Channel.CreateBounded<byte[]>(10);
        // Register this channel with the runtime based on this stream id
        streamTracker.AddStream(invocation.StreamIds[0], item => channel2.Writer.WriteAsync((byte[])item), (Exception ex) => channel2.Writer.TryComplete(ex));
        var stream2 = channel2.Reader.ReadAllAsync();
        var args = invocation.Arguments;
        try
        {
            await ((ChatHub)hub).UploadData((string)args[0], (System.DateTime)args[1], stream2);
        }
        catch (Exception ex) when (invocation.InvocationId is not null)
        {
            await connection.WriteAsync(Microsoft.AspNetCore.SignalR.Protocol.CompletionMessage.WithError(invocation.InvocationId, "Invoking UploadData failed"));
            return;
        }
        finally
        {
            channel2.Writer.TryComplete();
            // Unregister this channel with the runtime based on this stream id
            streamTracker.RemoveStream(invocation.StreamIds[0]);
            
        }
        
        if (invocation.InvocationId is not null)
        {
            await connection.WriteAsync(Microsoft.AspNetCore.SignalR.Protocol.CompletionMessage.WithResult(invocation.InvocationId, null));
        }
    }
    
    public static void InitializeHub(Microsoft.AspNetCore.SignalR.Hub hub, Microsoft.AspNetCore.SignalR.HubConnectionContext connection, Microsoft.AspNetCore.SignalR.IHubCallerClients clients)
    {
        // We need to wrap the original
        ((ChatHub)hub).Clients = new ChatHubClientsImpl(clients);
    }
    
    private class IClientImpl : IClient
    {
        private Microsoft.AspNetCore.SignalR.IClientProxy Proxy { get; }
        public IClientImpl(Microsoft.AspNetCore.SignalR.IClientProxy proxy) => Proxy = proxy;
        public System.Threading.Tasks.Task Send(string message) => Proxy.SendCoreAsync("Send", new object[] {message});
    }
    
    private class ChatHubClientsImpl : Microsoft.AspNetCore.SignalR.IHubCallerClients<IClient>
    {
        private readonly Microsoft.AspNetCore.SignalR.IHubCallerClients _clients;
        public ChatHubClientsImpl(Microsoft.AspNetCore.SignalR.IHubCallerClients clients) => _clients = clients;
        
        public IClient All => new IClientImpl(_clients.All);
        
        public IClient Caller => new IClientImpl(_clients.Caller);
        
        public IClient Others => new IClientImpl(_clients.Others);
        
        public IClient AllExcept(System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds) => new IClientImpl(_clients.AllExcept(excludedConnectionIds));
        public IClient Client(string connectionId) => new IClientImpl(_clients.Client(connectionId));
        public IClient Clients(System.Collections.Generic.IReadOnlyList<string> connectionIds) => new IClientImpl(_clients.Clients(connectionIds));
        public IClient Group(string groupName) => new IClientImpl(_clients.Group(groupName));
        public IClient Groups(System.Collections.Generic.IReadOnlyList<string> groupNames) => new IClientImpl(_clients.Groups(groupNames));
        public IClient GroupExcept(string groupName, System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds) => new IClientImpl(_clients.GroupExcept(groupName,excludedConnectionIds));
        public IClient User(string userId) => new IClientImpl(_clients.User(userId));
        public IClient Users(System.Collections.Generic.IReadOnlyList<string> userIds) => new IClientImpl(_clients.Users(userIds));
        public IClient OthersInGroup(string groupName) => new IClientImpl(_clients.OthersInGroup(groupName));
        
    }
    
    public static void BindHub(Microsoft.AspNetCore.SignalR.IHubDefinition definition)
    {
        definition.SetHubInitializer(InitializeHub);
        definition.AddHubMethod("Send", SendThunk);
        definition.AddHubMethod("Loop", LoopThunk);
        definition.AddHubMethod("UploadData", UploadDataThunk);
    }
}
namespace SourceGeneratorPlayground
{
    public partial class MessagesHub
    {
        static async Task GroupSendThunk(Microsoft.AspNetCore.SignalR.Hub hub, Microsoft.AspNetCore.SignalR.HubConnectionContext connection, Microsoft.AspNetCore.SignalR.IStreamTracker streamTracker, Microsoft.AspNetCore.SignalR.Protocol.HubMessage message, System.Threading.CancellationToken cancellationToken)
        {
            var invocation = (Microsoft.AspNetCore.SignalR.Protocol.InvocationMessage)message;
            var args = invocation.Arguments;
            try
            {
                await ((SourceGeneratorPlayground.MessagesHub)hub).GroupSend((string)args[0], (string)args[1]);
            }
            catch (Exception ex) when (invocation.InvocationId is not null)
            {
                await connection.WriteAsync(Microsoft.AspNetCore.SignalR.Protocol.CompletionMessage.WithError(invocation.InvocationId, "Invoking GroupSend failed"));
                return;
            }
            finally
            {
            }
            
            if (invocation.InvocationId is not null)
            {
                await connection.WriteAsync(Microsoft.AspNetCore.SignalR.Protocol.CompletionMessage.WithResult(invocation.InvocationId, null));
            }
        }
        
        static async Task EchoNumbersThunk(Microsoft.AspNetCore.SignalR.Hub hub, Microsoft.AspNetCore.SignalR.HubConnectionContext connection, Microsoft.AspNetCore.SignalR.IStreamTracker streamTracker, Microsoft.AspNetCore.SignalR.Protocol.HubMessage message, System.Threading.CancellationToken cancellationToken)
        {
            var invocation = (Microsoft.AspNetCore.SignalR.Protocol.StreamInvocationMessage)message;
            var channel0 = System.Threading.Channels.Channel.CreateBounded<int>(10);
            // Register this channel with the runtime based on this stream id
            streamTracker.AddStream(invocation.StreamIds[0], item => channel0.Writer.WriteAsync((int)item), (Exception ex) => channel0.Writer.TryComplete(ex));
            var stream0 = channel0.Reader.ReadAllAsync();
            var args = invocation.Arguments;
            var streamItemMessage = new Microsoft.AspNetCore.SignalR.Protocol.StreamItemMessage(invocation.InvocationId, null);
            try
            {
                await foreach (var item in ((SourceGeneratorPlayground.MessagesHub)hub).EchoNumbers(stream0, cancellationToken).WithCancellation(cancellationToken))
                {
                    streamItemMessage.Item = item;
                    await connection.WriteAsync(streamItemMessage);
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                channel0.Writer.TryComplete();
                // Unregister this channel with the runtime based on this stream id
                streamTracker.RemoveStream(invocation.StreamIds[0]);
            }
        }
        
        public static void BindHub(Microsoft.AspNetCore.SignalR.IHubDefinition definition)
        {
            definition.AddHubMethod("GroupSend", GroupSendThunk);
            definition.AddHubMethod("EchoNumbers", EchoNumbersThunk);
        }
    }
}