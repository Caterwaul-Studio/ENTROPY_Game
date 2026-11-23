using UnityEngine;

public static class MapPartValues
{
    public static readonly int FadeId = Shader.PropertyToID("_Fade");
    public static readonly int PowerId = Shader.PropertyToID("_Power");
    public static readonly int UnpoweredBrightnessId = Shader.PropertyToID("_UnpoweredBrightness");
    public static readonly int UnpoweredSaturationId = Shader.PropertyToID("_UnpoweredSaturation");

    // Unity can't create this while loading a class
    private static MaterialPropertyBlock block;

    public static void SetFade(Renderer part, float fade)
    {
        Read(part);
        block.SetFloat(FadeId, fade);
        Write(part);
    }

    public static void SetPower(Renderer part, float power, float unpoweredBrightness, float unpoweredSaturation)
    {
        Read(part);
        block.SetFloat(PowerId, power);
        block.SetFloat(UnpoweredBrightnessId, unpoweredBrightness);
        block.SetFloat(UnpoweredSaturationId, unpoweredSaturation);
        Write(part);
    }

    private static void Read(Renderer part)
    {
        if (block == null) block = new MaterialPropertyBlock();
        part.GetPropertyBlock(block);
    }

    private static void Write(Renderer part)
    {
        if (IsFull(FadeId) && IsFull(PowerId)) part.SetPropertyBlock(null);
        else part.SetPropertyBlock(block);
    }

    private static bool IsFull(int id)
    {
        return !block.HasFloat(id) || block.GetFloat(id) >= 1f;
    }
}
