using System;
using Celeste.Mod.ExampleCollab.Mechanism;
using Monocle;

namespace Celeste.Mod.ExampleCollab;

/// <summary>
/// 合集代码 mod 的入口。Everest 通过 everest.yaml 里的 DLL 找到这里。
/// 只放生命周期和全局状态，具体逻辑丢到 Mechanism / Entities 下。
/// </summary>
public class ExampleCollabModule : EverestModule
{
    public static ExampleCollabModule Instance { get; private set; } = null!;

    public override Type SettingsType => typeof(ExampleCollabSettings);
    public static ExampleCollabSettings Settings => (ExampleCollabSettings)Instance._Settings;

    public override Type SessionType => typeof(ExampleCollabSession);
    public static ExampleCollabSession Session => (ExampleCollabSession)Instance._Session;

    public override Type SaveDataType => typeof(ExampleCollabSaveData);
    public static ExampleCollabSaveData SaveData => (ExampleCollabSaveData)Instance._SaveData;

    public ExampleCollabModule()
    {
        Instance = this;
#if DEBUG
        Logger.SetLogLevel(nameof(ExampleCollabModule), LogLevel.Verbose);
#else
        Logger.SetLogLevel(nameof(ExampleCollabModule), LogLevel.Info);
#endif
    }

    public override void Load()
    {
        // 先初始化各种 manager，再让它们各自挂 hook
        CollabEntityManager.Load();
        CollabEffectManager.Load();
    }

    public override void LoadContent(bool firstLoad)
    {
        base.LoadContent(firstLoad);
        CollabEffectManager.LoadContent();
    }

    public override void Unload()
    {
        CollabEffectManager.UnLoad();
        CollabEntityManager.UnLoad();
    }
}
