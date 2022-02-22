using DFrame;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


public class SampleOne : MonoBehaviour
{
    DFrameWorkerApp app;

    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        new GameObject("DFrame Worker", typeof(SampleOne));
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        app = new DFrameWorkerApp("localhost:7313");
        await app.Run();
    }

    private void OnDestroy()
    {
        app.Dispose();
    }
}

[Preserve]
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

// Preserve for Unity IL2CPP

internal class PreserveAttribute : System.Attribute
{
}