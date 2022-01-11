
/// <summary>
/// 收到请求事件
/// </summary>
public enum ReceivedEventType
{
    /// <summary>
    /// 默认
    /// </summary>
    None,

    /// <summary>
    /// 关闭
    /// </summary>
    Exit,

    /// <summary>
    /// 场景运行参数
    /// </summary>
    SceneData,

    /// <summary>
    /// 场景启动
    /// </summary>
    SceneStart,

    /// <summary>
    /// 训练数据
    /// </summary>
    TrainingData,

    /// <summary>
    /// 重置场景
    /// </summary>
    ResetScene,
}
