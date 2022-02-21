using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Server;
using MessagePack;
using MinimalMagicOnion.Services;
using ZLogger;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddMagicOnion();


builder.Logging.ClearProviders();
builder.Logging.AddZLoggerConsole();

var app = builder.Build();

// Configure the HTTP request pipeline.
//app.MapGrpcService<GreeterService>();
app.MapMagicOnionService();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();


public class EchoService : ServiceBase<IEchoService>, IEchoService
{
    public UnaryResult<Nil> Echo(string message)
    {
        return UnaryResult(Nil.Default);
    }
}

// unary
public interface IEchoService : IService<IEchoService>
{
    UnaryResult<Nil> Echo(string message);
}

// streaming hub
public interface IEchoHubReceiver
{
    void OnSend(MessageResponse message);
}
public class EchoReceiver : IEchoHubReceiver
{
    readonly GrpcChannel channel;

    public EchoReceiver(GrpcChannel channel)
    {
        this.channel = channel;
    }

    public IEchoHub Client { get; set; } = default!;

    public async void OnSend(MessageResponse message)
    {
        //Console.WriteLine("Reciever:" + message.Message);
        await Client.EchoAsync(message.Message);
    }
}

public interface IEchoHub : IStreamingHub<IEchoHub, IEchoHubReceiver>
{
    Task<MessageResponse> EchoAsync(string message);
    Task<MessageResponse> EchoBroadcastAsync(string message);
}

[MessagePackObject]
public struct MessageResponse
{
    [Key(0)]
    public string Message { get; set; }
}