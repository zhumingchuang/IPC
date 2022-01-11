using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcessHelper
{
    public class IpcInterface : System.IDisposable
    {
        public IpcServerInterface ipcServerInterface { get; }
        public IpcClientInterface ipcClientInterface { get; }

        public IpcInterface(int port, int partnerPort)
        {
            ipcServerInterface = new IpcServerInterface(port);
            ipcClientInterface = new IpcClientInterface(partnerPort);
        }

        public void Dispose()
        {
            ipcServerInterface.Dispose();
            ipcClientInterface.Dispose();
            GC.SuppressFinalize(this);
        }

    }
}
