using DFrame;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SampleOne : MonoBehaviour
{
    DFrameWorkerApp app;

    async void Start()
    {
        app = new DFrameWorkerApp(new DFrameWorkerOptions("localhost:7313"));
        await app.Run();
    }

    private void OnDestroy()
    {
        app.Dispose();
    }
}


public class SampleWorkload : Workload
{
    public override Task ExecuteAsync(WorkloadContext context)
    {
        Debug.Log("Exec");
        return Task.CompletedTask;
    }

    public override Task TeardownAsync(WorkloadContext context)
    {
        Debug.Log("Teardown");
        return Task.CompletedTask;
    }
}