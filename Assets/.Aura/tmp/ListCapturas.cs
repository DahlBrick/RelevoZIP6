using System.IO;
using System.Text;

public static class ListCapturas
{
    public static void Execute()
    {
        string dir = @"C:\Users\pipef\AppData\Local\AuraForUnity\CapturedImages\b4523ddadae75dfd";
        var sb = new StringBuilder();
        if (!Directory.Exists(dir)) { UnityEngine.Debug.Log("NO EXISTE: " + dir); return; }
        foreach (var f in Directory.GetFiles(dir, "*.jpeg"))
        {
            var fi = new FileInfo(f);
            sb.AppendLine(fi.Name + "  " + fi.LastWriteTime.ToString("HH:mm:ss") + "  " + fi.Length);
        }
        UnityEngine.Debug.Log(sb.ToString());
    }
}
