using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

/// <summary>
/// 进程退出
/// </summary>
public class ProcessExitedHelper : IDisposable
{
    private int _processExitedRaised;
    private readonly Process _process;
    public int ProcessId { get; }

    public ProcessExitedHelper(int processId, Action<ProcessExitedHelper> processExited)
    {
        ProcessId = processId;
        _process = Process.GetProcesses().SingleOrDefault(pr => pr.Id == processId);
        if (_process == null)
        {
            UnityEngine.Debug.Log($"没有找到父进程{processId}");
            OnProcessExit();
            return;
        }
        UnityEngine.Debug.Log($"找到父进程{processId}");
        try
        {
            _process.EnableRaisingEvents = true;
            _process.Exited += (_, __) =>
            {
                UnityEngine.Debug.Log($"父进程{processId}已经退出");
                OnProcessExit();
            };
        }
        catch (Exception)
        {
            UnityEngine.Debug.Log($"父进程{processId}已经退出");
            OnProcessExit();
        }

        if (_process.HasExited)
        {
            UnityEngine.Debug.Log($"父进程{processId}已经退出");
            OnProcessExit();
        }

        void OnProcessExit()
        {
            if (Interlocked.CompareExchange(ref _processExitedRaised, 1, 0) == 0)
            {
                UnityEngine.Debug.Log("退出程序");
                processExited(this);
            }
        }
    }
    public void Dispose()
    {
        _process?.Dispose();
    }
}
