using System.Diagnostics;
/// <summary>
/// 进程信息
/// </summary>
internal class ProcessInfo : IProcessInfo
{
    private readonly Process _process;

    internal ProcessInfo(Process process)
    {
        _process = process;
    }

    public int ExitCode => _process.ExitCode;

    public int Id => _process.Id;
}