using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

using System.Drawing;
using System.Text.Json;

public static class HanAirDropGlow
{
    //设置发光 
    public static void SetGlow(CBaseEntity entity, int ColorA, int ColorR, int ColorG, int ColorB)
    {
        CBaseModelEntity? modelGlow = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
        CBaseModelEntity? modelRelay = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
        if (modelGlow == null || modelRelay == null)
            return;

        string modelName = entity.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName;

        modelRelay.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= unchecked((uint)~(1 << 2));

        modelRelay.SetModel(modelName);
        modelRelay.Spawnflags = 256u;
        modelRelay.RenderMode = RenderMode_t.kRenderNone;
        modelRelay.DispatchSpawn();

        modelGlow.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= unchecked((uint)~(1 << 2));

        modelGlow.SetModel(modelName);
        modelGlow.Spawnflags = 256u;
        modelGlow.DispatchSpawn();

        modelGlow.Glow.GlowColorOverride = Color.FromArgb(ColorA, ColorR, ColorG, ColorB); //Color.Magenta;
        modelGlow.Glow.GlowRange = 5000;
        modelGlow.Glow.GlowTeam = -1;
        modelGlow.Glow.GlowType = 3;
        modelGlow.Glow.GlowRangeMin = 100;

        modelRelay.AcceptInput("FollowEntity", entity, modelRelay, "!activator");
        modelGlow.AcceptInput("FollowEntity", modelRelay, modelGlow, "!activator");

    }

    public static bool TryParseColor(string colorStr, out Color color, Color defaultColor)
    {
        // 默认返回预设颜色
        color = defaultColor;

        // 1. 检查空值
        if (string.IsNullOrWhiteSpace(colorStr))
            return false;

        // 2. 分割字符串
        var parts = colorStr.Split(',');

        // 3. 检查最小长度
        if (parts.Length < 4)
            return false;

        // 严格按照ARGB顺序解析
        if (!TryParseColorComponent(parts[0], out int a) ||  // Alpha
            !TryParseColorComponent(parts[1], out int r) ||  // Red
            !TryParseColorComponent(parts[2], out int g) ||  // Green
            !TryParseColorComponent(parts[3], out int b))    // Blue
        {
            return false;
        }

        color = Color.FromArgb(a, r, g, b);
        return true;
    }

    // 辅助方法：解析单个颜色分量（0-255）
    public static bool TryParseColorComponent(string str, out int value)
    {
        value = 0;
        if (!int.TryParse(str, out int tmp) || tmp < 0 || tmp > 255)
            return false;

        value = tmp;
        return true;
    }
}
