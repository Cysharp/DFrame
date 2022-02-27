using DFrame.RestSdk;

var client = new DFrameClient("http://localhost:7312/");

// start request
await client.ExecuteRequestAsync(new()
{
    Workload = "SampleWorkload",
    Concurrency = 10,
    TotalRequest = 100000
});

// loadtest is running, wait complete.
await client.WaitUntilCanExecute();

// get summary and results[]
var result = await client.GetLatestResultAsync();


var c = await client.GetResultsListAsync();


var f = await client.GetResultAsync(c.First().ExecutionId);
if (f != null)
{
    foreach (var item in f.Results)
    {
        Console.WriteLine(item.WorkerId);
    }
}

//var r = await client.ExecuteRepeatAsync(new RepeatBody
//{
//    Workload = "SampleWorkload",
//    Concurrency = 2,
//    //Workerlimit = 1,
//    TotalRequest = 10000000,
//    Parameters = new()
//    {
//        { "world", "hello" }
//    },
//    RepeatCount = 10,
//    IncreaseTotalRequest = 10000000,
//    IncreaseTotalWorker = 10,
//});

//Console.WriteLine(r.ExecutionId);

