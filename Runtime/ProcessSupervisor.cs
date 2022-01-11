using Stateless;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessHelper
{
    public class ProcessSupervisor : IDisposable
    {
        public enum State
        {
            /// <summary>
            /// 没有运行
            /// </summary>
            NotStarted,
            /// <summary>
            /// 运行
            /// </summary>
            Running,
            /// <summary>
            /// 启动失败
            /// </summary>
            StartFailed,
            /// <summary>
            /// 停止
            /// </summary>
            Stopping,
            /// <summary>
            /// 成功退出
            /// </summary>
            ExitedSuccessfully,
            /// <summary>
            /// 退出错误
            /// </summary>
            ExitedWithError,
            /// <summary>
            /// 异常退出
            /// </summary>
            ExitedUnexpectedly,
            /// <summary>
            /// 已退出
            /// </summary>
            ExitedKilled
        }

        private enum Trigger
        {
            /// <summary>
            /// 启动
            /// </summary>
            Start,
            StartError,
            Stop,
            ProcessExit
        }

        /// <summary>
        /// 进程路径
        /// </summary>
        private readonly string _processPath;
        /// <summary>
        /// 参数
        /// </summary>
        private readonly string _arguments;
        private readonly bool _captureStdErr;

        /// <summary>
        /// 程序目录
        /// </summary>
        private readonly string _workingDirectory;
        private readonly StringDictionary _environmentVariables;
        private Process _process;
        private bool _killed;
        private readonly TaskQueue _taskQueue = new TaskQueue();

        /// <summary>
        /// 发送管理
        /// </summary>
        public IpcClientInterface ipcClientInterface { get; private set; }
        /// <summary>
        /// 启动进程信息
        /// </summary>
        public IProcessInfo ProcessInfo { get; private set; }

        /// <summary>
        /// 进程当前状态
        /// </summary>
        public State CurrentState => _processStateMachine.State;

        /// <summary>
        /// 进程启动失败异常
        /// </summary>
        public Exception OnStartException { get; private set; }

        /// <summary>
        ///  进程状态改变事件
        /// </summary>
        public event Action<State> StateChanged;

        /// <summary>
        /// 进程发出控制台数据
        /// </summary>
        public event Action<string> OutputDataReceived;

        /// <summary>
        ///  错误数据
        /// </summary>
        public event Action<string> ErrorDataReceived;

        private readonly StateMachine<State, Trigger> _processStateMachine = new StateMachine<State, Trigger>(State.NotStarted, FiringMode.Immediate);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Exception> _startErrorTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<TimeSpan?> _stopTrigger;

        public ProcessSupervisor(ProcessRunType processRunType, IpcClientInterface ipcClientInterface, string workingDirectory, string processPath, string arguments = null, StringDictionary environmentVariables = null, bool captureStdErr = false)
        {
            this.ipcClientInterface = ipcClientInterface;
            _workingDirectory = workingDirectory;
            _processPath = processPath;
            _arguments = arguments ?? string.Empty;
            _environmentVariables = environmentVariables;
            _captureStdErr = captureStdErr;

            //配置运行状态
            _processStateMachine.Configure(State.NotStarted)
                .Permit(Trigger.Start, State.Running);
            //启动错误
            _startErrorTrigger = _processStateMachine.SetTriggerParameters<Exception>(Trigger.StartError);
            //关闭进程
            _stopTrigger = _processStateMachine.SetTriggerParameters<TimeSpan?>(Trigger.Stop);

            _processStateMachine.Configure(State.Running)
                .OnEntryFrom(Trigger.Start, OnStart)
                .PermitIf(Trigger.ProcessExit, State.ExitedSuccessfully, () => processRunType == ProcessRunType.SelfTerminating && _process.HasExited && _process.ExitCode == 0)
                .PermitIf(Trigger.ProcessExit, State.ExitedWithError, () => processRunType == ProcessRunType.SelfTerminating && _process.HasExited && _process.ExitCode != 0)
                .PermitIf(Trigger.ProcessExit, State.ExitedUnexpectedly, () => processRunType == ProcessRunType.NonTerminating && _process.HasExited)
                .Permit(Trigger.Stop, State.Stopping)
                .Permit(Trigger.StartError, State.StartFailed);

            _processStateMachine.Configure(State.StartFailed)
                .OnEntryFrom(_startErrorTrigger, OnStartError);

            _processStateMachine.Configure(State.Stopping)
                .OnEntryFromAsync(_stopTrigger, OnStop)
                .PermitIf(Trigger.ProcessExit, State.ExitedSuccessfully, () => processRunType == ProcessRunType.NonTerminating && !_killed && _process.HasExited && _process.ExitCode == 0)
                .PermitIf(Trigger.ProcessExit, State.ExitedWithError, () => processRunType == ProcessRunType.NonTerminating && !_killed && _process.HasExited && _process.ExitCode != 0)
                .PermitIf(Trigger.ProcessExit, State.ExitedKilled, () => processRunType == ProcessRunType.NonTerminating && _killed && _process.HasExited && _process.ExitCode != 0);

            _processStateMachine.Configure(State.StartFailed)
                .Permit(Trigger.Start, State.Running);

            _processStateMachine.Configure(State.ExitedSuccessfully)
                .Permit(Trigger.Start, State.Running);

            _processStateMachine.Configure(State.ExitedUnexpectedly)
                .Permit(Trigger.Start, State.Running);

            _processStateMachine.Configure(State.ExitedKilled)
                .Permit(Trigger.Start, State.Running);

            _processStateMachine.OnTransitioned(transition =>
            {
                StateChanged?.Invoke(transition.Destination);
            });
        }

        /// <summary>
        /// 开始进程
        /// </summary>
        public Task Start()
            => _taskQueue.Enqueue(() =>
            {
                _killed = false;
                _processStateMachine.Fire(Trigger.Start);
            });

        private void OnStart()
        {
            OnStartException = null;
            try
            {
                var processStartInfo = new ProcessStartInfo(_processPath)
                {
                    Arguments = _arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = _captureStdErr,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = _workingDirectory
                };

                if (_environmentVariables != null)
                {
                    foreach (string key in _environmentVariables.Keys)
                    {
                        processStartInfo.EnvironmentVariables[key] = _environmentVariables[key];
                    }
                }
                _process = new Process
                {
                    StartInfo = processStartInfo,
                    EnableRaisingEvents = true
                };
                _process.OutputDataReceived += (_, args) => OutputDataReceived?.Invoke(args.Data);
                if (_captureStdErr)
                {
                    _process.ErrorDataReceived += (_, args) => ErrorDataReceived?.Invoke(args.Data);
                }
                _process.Exited += (sender, args) =>
                {
                    _taskQueue.Enqueue(() =>
                    {
                        _processStateMachine.Fire(Trigger.ProcessExit);
                    });
                };
                _process.Start();
                _process.BeginOutputReadLine();
                if (_captureStdErr)
                {
                    _process.BeginErrorReadLine();
                }

                ProcessInfo = new ProcessInfo(_process);
            }
            catch (Exception ex)
            {
                _processStateMachine.Fire(_startErrorTrigger, ex);
            }
        }

        /// <summary>
        /// 关闭进程
        /// </summary>
        public async Task Stop(TimeSpan? timeout = null)
        {
            await await _taskQueue.Enqueue(() => _processStateMachine.FireAsync(_stopTrigger, timeout)).ConfigureAwait(false);
        }

        /// <summary>
        /// 关闭进程 
        /// </summary>
        private async Task OnStop(TimeSpan? timeout)
        {
            if (!timeout.HasValue || timeout.Value <= TimeSpan.Zero)
            {
                try
                {
                    UnityEngine.Debug.Log($"关闭进程{_process.Id}");
                    _killed = true;
                    _process.Kill();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.Log(ex);
                    UnityEngine.Debug.Log("在已经退出进程并试图关闭");
                }
            }
            else
            {
                try
                {
                    var exited = WhenStateIs(State.ExitedSuccessfully);
                    var exitedWithError = WhenStateIs(State.ExitedWithError);
                    CooperativeShutdown.SignalExit(ipcClientInterface);

                    await Task.WhenAny(exited, exitedWithError)
                        .TimeoutAfter(timeout.Value)
                        .ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    try
                    {
                        _killed = true;
                        _process.Kill();
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning(ex);
                        UnityEngine.Debug.Log($"关闭进程{_process.Id}异常");
                    }
                }
            }
        }

        /// <summary>
        ///  等待状态
        /// </summary>
        public Task WhenStateIs(State processState, CancellationToken cancellationToken = default)
        {
            var taskCompletionSource = new TaskCompletionSource<int>();
            cancellationToken.Register(() => taskCompletionSource.TrySetCanceled());
            void Handler(State state)
            {
                if (processState == state)
                {
                    taskCompletionSource.SetResult(0);
                    StateChanged -= Handler;
                }
            }
            StateChanged += Handler;
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 启动错误
        /// </summary>
        private void OnStartError(Exception ex)
        {
            OnStartException = ex;
            _process?.Dispose();
            ProcessInfo = null;
#if UNITY_EDITOR
            UnityEngine.Debug.Log(ex);
#endif
        }

        public void Dispose()
        {
            _process?.Dispose();
            _taskQueue?.Dispose();
            ipcClientInterface.Dispose();
        }

    }
}
