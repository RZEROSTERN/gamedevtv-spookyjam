using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using UnityEngine;
using Uninvited;

// Fallback for a machine whose Unity editor cannot start. Unity's own generator
// remains the canonical scene setup tool once the editor is available.
static class BootstrapAssets
{
    static string GuidFor(string path)
    {
        foreach (var line in File.ReadLines(path + ".meta"))
            if (line.StartsWith("guid: ")) return line.Substring(6).Trim();
        throw new InvalidOperationException("Missing metadata: " + path);
    }
    public static void Write()
    {
        const string balance = "Assets/Uninvited/Data/GameBalance.asset";
        const string scene = "Assets/Uninvited/Scenes/Uninvited.unity";
        if (File.Exists(balance) || File.Exists(scene))
            throw new InvalidOperationException("Fallback refuses to overwrite an existing scene or balance.");
        var config = (GameConfig)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GameConfig));
        config.SetDefaults();
        var sb = new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n");
        sb.AppendLine("  m_Script: {fileID: 11500000, guid: " + GuidFor("Assets/Uninvited/Runtime/GameConfig.cs") + ", type: 3}");
        sb.AppendLine("  m_Name: GameBalance\n  m_EditorClassIdentifier:");
        WriteFields(sb, config, 2);
        File.WriteAllText(balance, sb.ToString());
        var assetGuid = "9c7ab66c4c0e4906882c413d1f013b32";
        File.WriteAllText(balance + ".meta", "fileFormatVersion: 2\nguid: " + assetGuid + "\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n");
        var scriptGuid = GuidFor("Assets/Uninvited/Runtime/GameBootstrap.cs");
        File.WriteAllText(scene, "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n" +
            "--- !u!1 &1000\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n  - component: {fileID: 1001}\n  - component: {fileID: 1002}\n  m_Layer: 0\n  m_Name: Uninvited Game\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n" +
            "--- !u!4 &1001\nTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 1000}\n  serializedVersion: 2\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_ConstrainProportionsScale: 0\n  m_Children: []\n  m_Father: {fileID: 0}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n" +
            "--- !u!114 &1002\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 1000}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: " + scriptGuid + ", type: 3}\n  m_Name:\n  m_EditorClassIdentifier:\n  config: {fileID: 11400000, guid: " + assetGuid + ", type: 2}\n" +
            "--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n  - {fileID: 1001}\n");
        File.WriteAllText(scene + ".meta", "fileFormatVersion: 2\nguid: 120c52dc3351407c98982868e79be2db\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n");
        Console.WriteLine("Fallback scene and balance written. Unity import and runtime validation are still required.");
    }
    static void WriteFields(StringBuilder sb, object value, int indent)
    {
        foreach (var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            sb.Append(' ', indent).Append(field.Name).Append(':');
            WriteValue(sb, field.GetValue(value), indent);
        }
    }
    static void WriteValue(StringBuilder sb, object value, int indent)
    {
        if (value is EnemyKind[] sequence)
        {
            sb.Append(' ');
            foreach (var kind in sequence)
                foreach (var b in BitConverter.GetBytes((int)kind)) sb.Append(b.ToString("x2"));
            sb.AppendLine(); return;
        }
        if (value is Array array)
        {
            sb.AppendLine();
            foreach (var entry in array)
            {
                sb.Append(' ', indent).Append('-');
                if (entry is Vector2 vector)
                    sb.AppendLine(" {x: " + vector.x.ToString(CultureInfo.InvariantCulture) + ", y: " + vector.y.ToString(CultureInfo.InvariantCulture) + "}");
                else
                {
                    var nested = new StringBuilder();
                    WriteFields(nested, entry, indent + 2);
                    var text = nested.ToString();
                    sb.Append(' ').Append(text.Substring(indent + 2));
                }
            }
            return;
        }
        if (value is string textValue) sb.Append(' ').AppendLine(JsonSerializer.Serialize(textValue));
        else if (value is Enum) sb.Append(' ').AppendLine(Convert.ToInt32(value).ToString());
        else sb.Append(' ').AppendLine(Convert.ToString(value, CultureInfo.InvariantCulture));
    }
}
