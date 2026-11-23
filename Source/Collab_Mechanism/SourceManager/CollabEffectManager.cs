using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.ExampleCollab.Mechanism;

/// <summary>
/// 合集自定义特效的加载器。
/// HLSL 源码放 Effects/&lt;合集&gt;_Source/，编译出来的 .cso 放 Effects/，
/// 名字要一一对应，这里按名字加载。
/// </summary>
public static class CollabEffectManager
{
    /// <summary>要加载的特效名单。加特效只改这里，别在实体里硬写路径。</summary>
    private static readonly string[] EffectNames =
    {
        "ExampleCollab_TintOverlay",
    };

    private static readonly Dictionary<string, Effect> Effects = new(StringComparer.Ordinal);

    public static bool Loaded { get; private set; }

    public static void Load()
    {
        // 这里只挂 hook，真正的 Effect 要等 GraphicsDevice 就绪，放 LoadContent
    }

    public static void LoadContent()
    {
        foreach (string name in EffectNames)
        {
            LoadEffect(name);
        }

        Loaded = true;
    }

    /// <summary>取一个特效；没编译 .cso 时返回 null，调用方要判空。</summary>
    public static Effect? Get(string name)
    {
        return Effects.TryGetValue(name, out Effect? effect) ? effect : null;
    }

    private static void LoadEffect(string name)
    {
        if (Effects.ContainsKey(name))
        {
            return;
        }

        if (!Everest.Content.TryGet($"Effects/{name}.cso", out ModAsset? asset) || asset is null)
        {
            Logger.Log(LogLevel.Warn, nameof(ExampleCollabModule), $"找不到特效 Effects/{name}.cso，跳过（HLSL 要先编译）");
            return;
        }

        Effects[name] = new Effect(Engine.Graphics.GraphicsDevice, asset.Data);
    }

    public static void UnLoad()
    {
        foreach (Effect effect in Effects.Values)
        {
            effect.Dispose();
        }

        Effects.Clear();
        Loaded = false;
    }
}
