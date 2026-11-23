namespace Celeste.Mod.ExampleCollab;

/// <summary>
/// 跟着存档里当前关卡走的临时状态，切图就没了。
/// 跨地图但不想进存档的东西（比如“这次进大厅是不是刚打完心门”）放这里。
/// </summary>
public class ExampleCollabSession : EverestModuleSession
{
    /// <summary>刚刚从哪张图回到大厅，用于大厅的入场演出。</summary>
    public string? LastMapReturnedFrom { get; set; }

    /// <summary>本次进大厅是否需要播放心门解锁演出。</summary>
    public bool PendingHeartDoorCutscene { get; set; }
}
