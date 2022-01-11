using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Threading.Tasks;
using System;

namespace ProcessHelper
{
    /// <summary>
    /// 进程启动管理
    /// </summary>
    public class ServerProcessManager : IDisposable
    {
        private IpcServerInterface _ipcInterface;
        public IpcServerInterface IpcInterface
        {
            get
            {
                if (_ipcInterface == null)
                {
                    _ipcInterface = new IpcServerInterface();
                }
                return _ipcInterface;
            }
        }
        private Dictionary<string, ProcessSupervisor> dictProcess;
        public Dictionary<string, ProcessSupervisor> DictProcess
        {
            get
            {
                if (dictProcess == null) dictProcess = new Dictionary<string, ProcessSupervisor>();
                return dictProcess;
            }
        }

        /// <summary>
        /// 初始化进程
        /// </summary>
        public ProcessSupervisor InitIpcService(ProcessRunType processRunType, string workingDirectory, string processPath, string arguments = null, StringDictionary environmentVariables = null, bool captureStdErr = false)
        {
            IpcClientInterface ipcClientInterface = new IpcClientInterface(GetPort());
            arguments += $"{ProcessParameter.ParentProcessPort}=={IpcInterface.Port} ";
            arguments += $"{ProcessParameter.ParentProcessPid}=={Process.GetCurrentProcess().Id} ";
            arguments += $"{ProcessParameter.ChildPort}=={ipcClientInterface.PartnerPort} ";
            var processName = Path.GetFileNameWithoutExtension(processPath);
            UnityEngine.Debug.Log(arguments);
            var supervisor = new ProcessSupervisor(processRunType, ipcClientInterface, workingDirectory, processPath, arguments, environmentVariables, captureStdErr);
            DictProcess.Add(processName, supervisor);
            return supervisor;
        }

        /// <summary>
        /// 检查端口是否可用
        /// </summary>
        private int GetPort()
        {
            var childPort = FreePortHelper.GetFreePort();
            foreach (var item in DictProcess)
            {
                if (item.Value.ipcClientInterface.PartnerPort == childPort) return GetPort();
            }
            return childPort;
        }

        public void Dispose()
        {
            _ipcInterface.Dispose();
            foreach (var item in DictProcess)
            {
                item.Value.Dispose();
            }
            DictProcess.Clear();
        }

    }
}
