namespace Celeste.Mod.ExampleCollab;

/// <summary>
/// 玩家能在 Mod Options 里改的设置。字段只在真正需要给玩家开关时才加，
/// 调试用的开关放这里反而会把设置页塞满。
/// </summary>
public class ExampleCollabSettings : EverestModuleSettings
{
    /// <summary>调试用：在屏幕上显示当前房间的 flag。</summary>
    public bool ShowDebugFlags { get; set; } = false;

    /// <summary>合集共用的主音量偏移，-1 到 1。</summary>
    public float AudioOffset { get; set; } = 0f;
}
