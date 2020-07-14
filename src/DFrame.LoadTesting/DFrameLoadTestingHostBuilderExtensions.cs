using ConsoleAppFramework;
using DFrame.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame
{
    public static class DFrameLoadTestingHostBuilderExtensions
    {
        public static async Task RunDFrameLoadTestingAsync(this IHostBuilder hostBuilder, string[] args, DFrameOptions options)
        {
            options.OnExecuteResult = SummaryResult;

            await hostBuilder.RunDFrameAsync(args, options);

        }

        static void SummaryResult(ExecuteResult[] results, DFrameOptions options)
        {
            // TODO:req/secとか色々集計したのを返す。
            // とりあえず集計したらConsole.WriteLine(雑)

            foreach (var item in results)
            {
                Console.WriteLine("Elapsed:" + item.Elapsed);
            }
        }
    }
}
