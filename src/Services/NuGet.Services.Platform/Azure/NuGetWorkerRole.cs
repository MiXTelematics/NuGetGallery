﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.WindowsAzure.ServiceRuntime;
using NuGet.Services.ServiceModel;

namespace NuGet.Services.Azure
{
    public abstract class NuGetWorkerRole : RoleEntryPoint
    {
        private AzureServiceHost _host;
        private Task _runTask;

        protected NuGetWorkerRole()
        {
            _host = new AzureServiceHost(this);
        }

        public override void Run()
        {
            try
            {
                _runTask = _host.Run();
                _runTask.Wait();
                ServicePlatformEventSource.Log.HostShutdownComplete(_host.Description.ServiceHostName.ToString());
            }
            catch (Exception ex)
            {
                ServicePlatformEventSource.Log.FatalException(ex);
                throw;
            }
        }

        public override void OnStop()
        {
            try
            {
                _host.Shutdown();

                // As per http://msdn.microsoft.com/en-us/library/microsoft.windowsazure.serviceruntime.roleentrypoint.onstop.aspx
                // We need to block the thread that's running OnStop until the shutdown completes.
                if (_runTask != null)
                {
                    _runTask.Wait();
                }
            }
            catch (Exception ex)
            {
                ServicePlatformEventSource.Log.FatalException(ex);
                throw;
            }
        }

        public override bool OnStart()
        {
            try
            {
                // Set the maximum number of concurrent connections 
                ServicePointManager.DefaultConnectionLimit = 12;

                // Initialize the host
                _host.Initialize().Wait();

                return _host.StartAndWait();
            }
            catch (Exception ex)
            {
                ServicePlatformEventSource.Log.FatalException(ex);
                throw;
            }
        }

        protected internal abstract IEnumerable<NuGetService> GetServices(ServiceHost host);
    }
}
