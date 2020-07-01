using Microsoft.Extensions.Hosting;
using MagicOnion;
using MagicOnion.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;
using MagicOnion.Server;
using System.Threading.Tasks;

namespace DFrame.Core
{
    public static class DFrameApp
    {
        public static async Task RunAsync(string[] args)
        {



            // get port and type.

            var t = Type.GetType("foobarbaz");
            var ports = new ServerPort("localhost", 123456, ServerCredentials.Insecure);

            Host.CreateDefaultBuilder(args)
                .UseMagicOnion(new[] { ports }, new MagicOnionOptions { }, types: new[] { t });






        }
    }

    public enum ScalingType
    {
        InProcess,
        OutOfProcess,
        Kubernetes,
    }

    public class EntryPoint
    {
        public void Main(int workerCount, int podCount, ScalingType scalingType, string groupName)
        {
        }
    }
}
