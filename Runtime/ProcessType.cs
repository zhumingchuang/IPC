namespace ProcessHelper
{

    public enum ProcessParameter
    {
        //路径
        Path,
        //父进程端口
        ParentProcessPort,
        //父进程ID
        ParentProcessPid,
        //子进程端口
        ChildPort,
    }

    /// <summary>
    /// 程序运行模式
    /// </summary>
    public enum ProcessRunType
    {
        /// <summary>
        /// 会自行终止的程序
        /// </summary>
        SelfTerminating,

        /// <summary>
        /// 不会自行终止的程序
        /// </summary>
        NonTerminating
    }
}