using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Deadweight > Export Generated Sounds: writes every built-in (synthesised) sound effect to
/// Assets/Audio/SFX_Generated/*.wav so you can listen to them, or edit one and use it as a replacement.
/// To replace a sound in the game, put a file with the same name in Assets/Audio/Resources/SFX/.
/// </summary>
public static class SfxExport
{
    public const string OutDir = "Assets/Audio/SFX_Generated";

    [MenuItem("Tools/Deadweight/Export Generated Sounds")]
    public static void Export()
    {
        Directory.CreateDirectory(OutDir);
        Dictionary<string, float[]> all = Sfx.RenderAll();
        foreach (KeyValuePair<string, float[]> kv in all) WriteWav(Path.Combine(OutDir, kv.Key + ".wav"), kv.Value, Sfx.SampleRate);
        AssetDatabase.Refresh();
        Debug.Log("[Sfx] Exported " + all.Count + " sounds to " + OutDir);
    }

    static void WriteWav(string path, float[] samples, int rate)
    {
        using (BinaryWriter w = new BinaryWriter(File.Create(path)))
        {
            int bytes = samples.Length * 2;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
            foreach (float f in samples) w.Write((short)Mathf.RoundToInt(Mathf.Clamp(f, -1f, 1f) * 32767f));
        }
    }
}
