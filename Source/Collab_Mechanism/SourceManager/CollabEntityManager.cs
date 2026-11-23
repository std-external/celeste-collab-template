using Monocle;

namespace Celeste.Mod.ExampleCollab.Mechanism;

/// <summary>
/// 合集的全局 hook 都挂在这里，别散在各个实体里。
/// 只放“所有地图都要”的行为；单张图的逻辑放那张图的实体。
/// </summary>
public static class CollabEntityManager
{
    private static bool loaded;

    public static void Load()
    {
        if (loaded)
        {
            return;
        }

        Everest.Events.Level.OnLoadLevel += OnLoadLevel;
        loaded = true;
    }

    public static void UnLoad()
    {
        if (!loaded)
        {
            return;
        }

        Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
        loaded = false;
    }

    private static void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
    {
        // 进图/进大厅都会走这里。示例：记一下玩家从哪张图回来，供大厅演出用。
        ExampleCollabModule.Session.LastMapReturnedFrom = level.Session.Area.GetSID();
    }
}
