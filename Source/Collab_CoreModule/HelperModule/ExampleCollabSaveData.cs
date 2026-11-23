using System.Collections.Generic;

namespace Celeste.Mod.ExampleCollab;

/// <summary>
/// 跟着存档走的长期数据。地图进度本身由 CollabUtils2 / 存档管，
/// 这里只放合集自己额外要记的东西（比如作者名单、看过的演出）。
/// 字段要能被 Everest 的存档序列化，别放引用类型的复杂结构。
/// </summary>
public class ExampleCollabSaveData : EverestModuleSaveData
{
    /// <summary>已经放过入场演出的地图 SID，避免每次进大厅都放一遍。</summary>
    public HashSet<string> SeenIntroMaps { get; set; } = new();

    /// <summary>玩家在合集里总共死过多少次，用于大厅的统计展示。</summary>
    public int TotalDeaths { get; set; }
}
