// 示例特效：给画面上叠一层颜色。
// 源码放 Effects/ExampleCollab_Source/，用 fxc 编译成 Effects/ExampleCollab_TintOverlay.cso。
// 编译命令见 Effects/README.md。
//
// 注意：Celeste 用的是 FNA，吃的是 fx_2_0 / ps_3_0 这一代 profile，
// 别直接抄 DBBHelper 里那些 ps_5_0 的写法。

sampler2D SpriteTexture : register(s0);

float4 TintColor = float4(1, 1, 1, 1);
float Opacity = 0.35;

float4 TintOverlay(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 sampled = tex2D(SpriteTexture, texCoord) * color;
    return lerp(sampled, TintColor, Opacity);
}

technique TintOverlay
{
    pass Pass0
    {
        PixelShader = compile ps_2_0 TintOverlay();
    }
}
