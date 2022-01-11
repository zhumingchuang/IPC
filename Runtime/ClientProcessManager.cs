using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ProcessHelper
{
    /// <summary>
    /// 进程管理
    /// </summary>
    public class ClientProcessManager:System.IDisposable
    {
        /// <summary>
        /// IPC通信
        /// </summary>
        public IpcInterface IpcInterface { get; private set; }

        public Action<ProcessExitedHelper> parentExited;
        public Action shutdownRequested;

        /// <summary>
        /// 获取运行参数
        /// </summary>
        public void GetRunProcessParameter(out Dictionary<ProcessParameter, string> dictParameter)
        {
            //获取启动参数
            string[] CommandLineArgs = Environment.GetCommandLineArgs();
            dictParameter = new Dictionary<ProcessParameter, string>();
            for (int i = 0; i < CommandLineArgs.Length; i++)
            {
                if (i == 0)
                {
                    dictParameter.Add(ProcessParameter.Path, CommandLineArgs[i]);
                }
                else
                {
                    string[] split = { "==" };
                    var parameter = CommandLineArgs[i].Split(split, StringSplitOptions.RemoveEmptyEntries);
                    dictParameter.Add((ProcessParameter)Enum.Parse(typeof(ProcessParameter), parameter[0]), parameter[1]);
                }
            }
        }

        /// <summary>
        /// 初始化IPC服务
        /// </summary>
        public void InitIpcClientService()
        {
            Dictionary<ProcessParameter, string> dictParameter;
            GetRunProcessParameter(out dictParameter);
            string parentPort, pid, childPort;
            dictParameter.TryGetValue(ProcessParameter.ParentProcessPort, out parentPort);
            dictParameter.TryGetValue(ProcessParameter.ParentProcessPid, out pid);
            dictParameter.TryGetValue(ProcessParameter.ChildPort, out childPort);
            int _parentPort, _pid, _childPort;
            int.TryParse(parentPort, out _parentPort);
            int.TryParse(pid, out _pid);
            int.TryParse(childPort, out _childPort);
            IpcInterface = new IpcInterface(_childPort, _parentPort);
            string str = $"服务器端口{parentPort}服务器ID{pid}本程序端口{childPort}";
            Debug.Log(str);
            var processExited = new ProcessExitedHelper(_pid, ParentExited);
            CooperativeShutdown.Listen(IpcInterface.ipcServerInterface, ShutdownRequested);
        }

        private void ParentExited(ProcessExitedHelper processExitedHelper)
        {
            Debug.Log("父进程关闭");
            parentExited?.Invoke(processExitedHelper);
        }

        private void ShutdownRequested()
        {
            Debug.Log("接收到关闭消息");
            shutdownRequested?.Invoke();
        }
        public void Dispose()
        {
            IpcInterface.Dispose();
        }
    }
}
