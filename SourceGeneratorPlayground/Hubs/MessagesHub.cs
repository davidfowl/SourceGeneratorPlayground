using Microsoft.AspNetCore.SignalR;

namespace SourceGeneratorPlayground
{
    public partial class MessagesHub : Hub
    {
        public Task GroupSend(string group, string message)
        {
            return Clients.Group(group).SendAsync(message);
        }

        public async IAsyncEnumerable<int> EchoNumbers(IAsyncEnumerable<int> message, CancellationToken cancellationToken)
        {
            await foreach (var item in message.WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
    }
}
