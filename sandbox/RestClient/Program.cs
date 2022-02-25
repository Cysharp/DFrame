
using DFrame.ResetSdk;

var client = new DFrameClient("http://localhost:7312/");

var c = await client.GetConnectionCountAsync();
Console.WriteLine(c);

await client.CancelAsync();

