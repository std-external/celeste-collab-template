# 特效（HLSL）怎么放、怎么编译

Celeste 是 FNA，只吃编译过的 Effect 二进制（`.cso` / `.fxb`），不吃 `.hlsl` 源码。
所以这里分两层：

```
Effects/
├─ ExampleCollab_Source/ExampleCollab_TintOverlay.hlsl   源码，进仓库
├─ ExampleCollab_TintOverlay.cso                         编译产物，随 mod 发
└─ README.md
```

## 命名

`<合集>_<作用>.hlsl` ↔ `<合集>_<作用>.cso`，两边同名。
`CollabEffectManager` 里的名单写的是不带后缀的名字（`ExampleCollab_TintOverlay`）。

## 编译

Windows 上装 DirectX SDK 或 Windows SDK，用 `fxc`：

```
fxc /T fx_2_0 /Fo Effects\ExampleCollab_TintOverlay.cso Effects\ExampleCollab_Source\ExampleCollab_TintOverlay.hlsl
```

- Celeste 用的是 fx_2_0 / ps_3_0 这一代，写 `ps_5_0` 的 shader 会加载失败。
- 在 Linux 上编译要套 Wine，或者干脆在 Windows 机器上编好把 `.cso` 提交上来。
- `.cso` 是二进制，别用文本编辑器改；改的是 `.hlsl`，改完重新编。

## 在代码里用

```csharp
Effect? effect = CollabEffectManager.Get("ExampleCollab_TintOverlay");
if (effect != null) {
    effect.Parameters["TintColor"]?.SetValue(new Vector4(1f, 0.4f, 0.2f, 1f));
    // 在 Draw 里 Apply / 换 RenderTarget
}
```

拿不到就返回 null（比如忘了编译），调用方判空，别让整张图崩掉。
