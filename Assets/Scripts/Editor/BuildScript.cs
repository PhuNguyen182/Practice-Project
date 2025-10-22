using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;

public class BuildScript
{
    public static void BuildWindows()
    {
        string targetPath = "Builds/Windows";
        CreateDirectory(targetPath);

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
        {
            scenes = GetSceneNames(),
            locationPathName = $"{targetPath}/Game.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        
        BuildPipeline.BuildPlayer(buildPlayerOptions);
        CreateZipBuild(targetPath);
    }
    
    private static void CreateDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }   
    }

    private static string[] GetSceneNames()
    {
        string[] sceneName = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        return sceneName;
    }

    private static void CreateZipBuild(string path)
    {
        string zipPath = $"{path}.zip";
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }
        
        ZipFile.CreateFromDirectory(path, zipPath);
    }
}
