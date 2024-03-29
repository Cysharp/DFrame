﻿namespace DFrame;

public class DFrameControllerOptions
{
    /// <summary>
    /// Affects to calculate median, percentile90, percentile95.
    /// </summary>
    public int CompleteElapsedBufferCount { get; set; } = 100000;

    public int ServerLogBufferCount { get; set; } = 1000;

    public string Title { get; set; } = "DFrame Controller";

    public bool DisableRestApi { get; set; } = false;
}
