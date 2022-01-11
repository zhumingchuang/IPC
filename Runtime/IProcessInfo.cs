/// <summary>
/// 进程信息
/// </summary>
public interface IProcessInfo
{
    /// <summary>
    /// 进程退出代码
    /// </summary>
    int ExitCode { get; }

    /// <summary>
    ///  进程ID
    /// </summary>
    int Id { get; }
}