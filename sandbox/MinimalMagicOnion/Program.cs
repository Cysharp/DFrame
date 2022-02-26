using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Server;
using MagicOnion.Server.Hubs;
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
}

public interface IEchoHub : IStreamingHub<IEchoHub, IEchoHubReceiver>
{
    Task<Nil> EchoAsync(string message);
}

public class EchoHub : StreamingHubBase<IEchoHub, IEchoHubReceiver>, IEchoHub
{
    public Task<Nil> EchoAsync(string message)
    {
        return Task.FromResult(Nil.Default);
    }
}
