using System;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;

using System.Timers;

namespace mmria.server;

public sealed class MMRIA_Window_Service  : System.ServiceProcess.ServiceBase
{


    protected override void OnStart(string[] args)
    {
        base.OnStart(args);
    }


    protected override void OnStop()
    {
        base.OnStop();
    }
}
/*
public static class MMRIA_Window_ServiceExtensions
{
    public static void RunAsCustomService(this IWebHost host)
    {
        var webHostService = new MMRIA_Window_Service(host);
        ServiceBase.Run(webHostService);
    }
} */

