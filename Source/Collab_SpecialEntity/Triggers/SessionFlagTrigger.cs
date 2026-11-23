using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ExampleCollab.Entities;

/// <summary>
/// 示例实体：玩家碰到就把一个 session flag 设成指定值。
/// 新的实体照这个来：一个文件一个类，构造签名固定是 (EntityData, Vector2 offset)。
/// Lönn 里显示的名字是 "ExampleCollab/SessionFlagTrigger"。
/// </summary>
[CustomEntity("ExampleCollab/SessionFlagTrigger")]
[Tracked]
public class SessionFlagTrigger : Trigger
{
    private readonly string flag;
    private readonly bool state;

    public SessionFlagTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        flag = data.Attr("flag", "ExampleCollab_flag");
        state = data.Bool("state", true);
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);

        Level level = SceneAs<Level>();
        level.Session.SetFlag(flag, state);
    }
}
