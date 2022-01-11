using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
namespace ProcessHelper
{
    public static class CooperativeShutdown
    {
        /// <summary>
        /// 监听关闭消息
        /// </summary>
        public static IDisposable Listen(IpcServerInterface ipcInterface, Action shutdownRequested, Action<Exception> onError = default)
        {
            var listener = new CooperativeShutdownListener(ipcInterface, shutdownRequested);
            try
            {
                listener.Listen();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(ex);
                onError?.Invoke(ex);
            }
            return listener;
        }

        /// <summary>
        /// 退出
        /// </summary>
        public static string SignalExit(IpcClientInterface ipcClientInterface)
        {
            return ipcClientInterface.SendMessage(string.Empty, ReceivedEventType.Exit);
        }

        private sealed class CooperativeShutdownListener : IDisposable
        {
            private readonly IpcServerInterface ipcServerInterface;
            private readonly Action shutdownRequested;
            internal CooperativeShutdownListener(IpcServerInterface ipcServerInterface, Action shutdownRequested)
            {
                this.ipcServerInterface = ipcServerInterface;
                this.shutdownRequested = shutdownRequested;
            }

            internal void Listen()
            {
                UnityEngine.Debug.Log("监听关闭消息");
                ipcServerInterface.AddListeningMessage(ReceivedEventType.Exit, ShutdownListener);
            }
            internal void ShutdownListener(object sender, IpcEventArgs args)
            {
                try
                {
                    shutdownRequested();
                }
                catch (Exception)
                {

                }
            }
            public void Dispose()
            {
                ipcServerInterface.RemoveListeningMessage(ReceivedEventType.Exit);
            }
        }
    }
}
