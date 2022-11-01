using Microsoft.AspNetCore.SignalR;

partial class ChatHub : Hub<IClient>
{
    public Task Send(string message)
    {
        return Clients.All.Send(message);
    }

    public async IAsyncEnumerable<object> Loop(int many, CancellationToken cancellationToken)
    {
        for (int i = 0; i < many; i++)
        {
            yield return i;
            await Task.Delay(1000, cancellationToken);
        }
    }

    public async Task UploadData(string name, DateTime date, IAsyncEnumerable<byte[]> blobs)
    {
        await foreach (var item in blobs)
        {

        }
    }
}

interface IClient
{
    Task Send(string message);
}