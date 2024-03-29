﻿using DFrame.Controller;
using MessagePipe;
using Microsoft.AspNetCore.Components;
using ObservableCollections;

namespace DFrame.Pages.Components;

public partial class Drawer : IDisposable
{
    [Inject] IScopedSubscriber<DrawerRequest> subscriber { get; set; } = default!;
    IDisposable? subscription;

    string? kind;
    string? title;
    bool isShow;
    Dictionary<string, string>? parameters;
    string? errorMessage;
    ISynchronizedView<string, string>? logView;
    Dictionary<WorkloadId, Dictionary<string, string>?>? results;

    protected override void OnInitialized()
    {
        subscription = subscriber.Subscribe(async x =>
        {
            kind = x.Kind;
            title = x.Title;
            isShow = x.IsShow;
            parameters = x.Parameters;
            errorMessage = x.ErrorMessage;
            logView = x.LogView;
            results = x.Results;

            await InvokeAsync(StateHasChanged);
        });
    }

    void Close()
    {
        isShow = false;
    }

    public void Dispose()
    {
        subscription?.Dispose();
    }
}

public record DrawerRequest(
    string? Kind,
    string? Title,
    bool IsShow,
    Dictionary<string, string>? Parameters,
    string? ErrorMessage, ISynchronizedView<string, string>? LogView,
    Dictionary<WorkloadId, Dictionary<string, string>?>? Results
);