using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// BuildScript cho Jenkins Pipeline
/// Hỗ trợ build: Windows, Android (APK/AAB), iOS
/// Unity Version: 6000.2.6f2
/// </summary>
public class BuildScript
{
    // ============================================
    // BUILD CONFIGURATIONS
    // ============================================
    
    private static string GetArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == $"-{name}" && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }
        return null;
    }

    private static void Log(string message)
    {
        Debug.Log($"[BuildScript] {message}");
        Console.WriteLine($"[BuildScript] {message}");
    }

    private static void LogError(string message)
    {
        Debug.LogError($"[BuildScript] ERROR: {message}");
        Console.WriteLine($"[BuildScript] ERROR: {message}");
    }

    // ============================================
    // WINDOWS BUILD
    // ============================================
    
    /// <summary>
    /// Build Windows Standalone
    /// Usage: -executeMethod BuildScript.BuildWindows -buildPath "Builds/Windows" -versionNumber "1.0.0" -buildNumber "1"
    /// </summary>
    [MenuItem("Build/Windows Standalone")]
    public static void BuildWindows()
    {
        Log("========================================");
        Log("🪟 Building Windows Standalone...");
        Log("========================================");

        try
        {
            // Lấy tham số từ command line
            string baseBuildPath = GetArgument("buildPath") ?? "Builds/Windows";
            string versionNumber = GetArgument("versionNumber") ?? PlayerSettings.bundleVersion;
            string buildNumber = GetArgument("buildNumber") ?? PlayerSettings.Android.bundleVersionCode.ToString();
            
            // Cập nhật version
            PlayerSettings.bundleVersion = versionNumber;
            
            // Tạo thư mục build theo version: Builds/Windows/1.0.0/
            string buildPath = Path.Combine(baseBuildPath, versionNumber);
            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }

            // Tên file executable
            string productName = PlayerSettings.productName;
            string buildFileName = $"{productName}.exe";
            string fullBuildPath = Path.Combine(buildPath, buildFileName);

            Log($"Build Path: {fullBuildPath}");
            Log($"Version: {versionNumber}");
            Log($"Build Number: {buildNumber}");

            // Lấy danh sách scenes
            string[] scenes = GetEnabledScenes();
            Log($"Scenes: {string.Join(", ", scenes)}");

            // Build options
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = fullBuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            // Thực hiện build
            Log("Building...");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            // Kiểm tra kết quả
            if (summary.result == BuildResult.Succeeded)
            {
                Log("========================================");
                Log($"✅ Windows Build SUCCEEDED!");
                Log($"Build size (from summary): {FormatBytes(summary.totalSize)}");
                Log($"Build size (actual file): {GetActualFileSize(fullBuildPath)}");
                Log($"Build time: {summary.totalTime}");
                Log($"Output: {fullBuildPath}");
                Log("========================================");
                EditorApplication.Exit(0);
            }
            else
            {
                LogError("========================================");
                LogError($"❌ Windows Build FAILED!");
                LogError($"Result: {summary.result}");
                LogError($"Errors: {summary.totalErrors}");
                LogError("========================================");
                EditorApplication.Exit(1);
            }
        }
        catch (Exception e)
        {
            LogError($"Exception during Windows build: {e.Message}");
            LogError(e.StackTrace);
            EditorApplication.Exit(1);
        }
    }

    // ============================================
    // ANDROID APK BUILD
    // ============================================
    
    /// <summary>
    /// Build Android APK
    /// Usage: -executeMethod BuildScript.BuildAndroidAPK -buildPath "Builds/Android" 
    ///        -keystorePath "path/to/keystore" -keystorePass "password" -keyaliasName "alias" -keyaliasPass "password"
    ///        -versionNumber "1.0.0" -buildNumber "1"
    /// </summary>
    [MenuItem("Build/Android APK")]
    public static void BuildAndroidAPK()
    {
        Log("========================================");
        Log("🤖 Building Android APK...");
        Log("========================================");

        try
        {
            // QUAN TRỌNG: Switch build target sang Android trước
            if (!SwitchToAndroidBuildTarget())
            {
                LogError("Cannot proceed with Android build. Exiting...");
                EditorApplication.Exit(1);
                return;
            }

            // Setup Android build
            SetupAndroidBuild();

            // Build APK (không phải AAB)
            EditorUserBuildSettings.buildAppBundle = false;

            // Lấy base path và version
            string baseBuildPath = GetArgument("buildPath") ?? "Builds/Android";
            string versionNumber = GetArgument("versionNumber") ?? PlayerSettings.bundleVersion;
            string productName = PlayerSettings.productName;
            
            // Tạo path theo version: Builds/Android/1.0.0/game.apk
            string versionPath = Path.Combine(baseBuildPath, versionNumber);
            if (!Directory.Exists(versionPath))
            {
                Directory.CreateDirectory(versionPath);
            }
            
            string buildPath = Path.Combine(versionPath, $"{productName}.apk");

            Log($"Build Path: {buildPath}");

            // Kiểm tra scenes
            string[] scenes = GetEnabledScenes();
            if (scenes == null || scenes.Length == 0)
            {
                LogError("❌ No scenes enabled in Build Settings!");
                LogError("Please add at least one scene to Build Settings:");
                LogError("  File → Build Settings → Add Open Scenes");
                EditorApplication.Exit(1);
                return;
            }
            
            Log($"Scenes to build ({scenes.Length}): {string.Join(", ", scenes)}");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Log("Building APK...");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            // Kiểm tra kết quả
            if (summary.result == BuildResult.Succeeded)
            {
                Log("========================================");
                Log($"✅ Android APK Build SUCCEEDED!");
                Log($"Build size (from summary): {FormatBytes(summary.totalSize)}");
                Log($"Build size (actual file): {GetActualFileSize(buildPath)}");
                Log($"Build time: {summary.totalTime}");
                Log($"Output: {buildPath}");
                
                // Verify file exists
                if (File.Exists(buildPath))
                {
                    FileInfo fileInfo = new FileInfo(buildPath);
                    Log($"✅ APK file verified: {fileInfo.Length} bytes");
                }
                else
                {
                    LogError($"⚠️  WARNING: APK file not found at expected path: {buildPath}");
                }
                
                Log("========================================");
                EditorApplication.Exit(0);
            }
            else
            {
                LogError("========================================");
                LogError($"❌ Android APK Build FAILED!");
                LogError($"Result: {summary.result}");
                LogError($"Errors: {summary.totalErrors}");
                
                // Log chi tiết errors nếu có
                if (report.steps != null)
                {
                    foreach (var step in report.steps)
                    {
                        if (step.messages != null)
                        {
                            foreach (var message in step.messages)
                            {
                                if (message.type == LogType.Error || message.type == LogType.Exception)
                                {
                                    LogError($"Build Error: {message.content}");
                                }
                            }
                        }
                    }
                }
                
                LogError("========================================");
                EditorApplication.Exit(1);
            }
        }
        catch (Exception e)
        {
            LogError($"Exception during Android APK build: {e.Message}");
            LogError(e.StackTrace);
            EditorApplication.Exit(1);
        }
    }

    // ============================================
    // ANDROID AAB BUILD
    // ============================================
    
    /// <summary>
    /// Build Android App Bundle (AAB)
    /// Usage: -executeMethod BuildScript.BuildAndroidAAB -buildPath "Builds/Android" 
    ///        -keystorePath "path/to/keystore" -keystorePass "password" -keyaliasName "alias" -keyaliasPass "password"
    ///        -versionNumber "1.0.0" -buildNumber "1"
    /// </summary>
    [MenuItem("Build/Android AAB")]
    public static void BuildAndroidAAB()
    {
        Log("========================================");
        Log("📦 Building Android App Bundle (AAB)...");
        Log("========================================");

        try
        {
            // QUAN TRỌNG: Switch build target sang Android trước
            if (!SwitchToAndroidBuildTarget())
            {
                LogError("Cannot proceed with Android build. Exiting...");
                EditorApplication.Exit(1);
                return;
            }

            // Setup Android build
            SetupAndroidBuild();

            // Build AAB
            EditorUserBuildSettings.buildAppBundle = true;

            // Lấy base path và version
            string baseBuildPath = GetArgument("buildPath") ?? "Builds/Android";
            string versionNumber = GetArgument("versionNumber") ?? PlayerSettings.bundleVersion;
            string productName = PlayerSettings.productName;
            
            // Tạo path theo version: Builds/Android/1.0.0/game.aab
            string versionPath = Path.Combine(baseBuildPath, versionNumber);
            if (!Directory.Exists(versionPath))
            {
                Directory.CreateDirectory(versionPath);
            }
            
            string buildPath = Path.Combine(versionPath, $"{productName}.aab");

            Log($"Build Path: {buildPath}");

            // Kiểm tra scenes
            string[] scenes = GetEnabledScenes();
            if (scenes == null || scenes.Length == 0)
            {
                LogError("❌ No scenes enabled in Build Settings!");
                LogError("Please add at least one scene to Build Settings:");
                LogError("  File → Build Settings → Add Open Scenes");
                EditorApplication.Exit(1);
                return;
            }
            
            Log($"Scenes to build ({scenes.Length}): {string.Join(", ", scenes)}");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Log("Building AAB...");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            // Kiểm tra kết quả
            if (summary.result == BuildResult.Succeeded)
            {
                Log("========================================");
                Log($"✅ Android AAB Build SUCCEEDED!");
                Log($"Build size (from summary): {FormatBytes(summary.totalSize)}");
                Log($"Build size (actual file): {GetActualFileSize(buildPath)}");
                Log($"Build time: {summary.totalTime}");
                Log($"Output: {buildPath}");
                
                // Verify file exists
                if (File.Exists(buildPath))
                {
                    FileInfo fileInfo = new FileInfo(buildPath);
                    Log($"✅ AAB file verified: {fileInfo.Length} bytes");
                }
                else
                {
                    LogError($"⚠️  WARNING: AAB file not found at expected path: {buildPath}");
                }
                
                Log("========================================");
                EditorApplication.Exit(0);
            }
            else
            {
                LogError("========================================");
                LogError($"❌ Android AAB Build FAILED!");
                LogError($"Result: {summary.result}");
                LogError($"Errors: {summary.totalErrors}");
                
                // Log chi tiết errors nếu có
                if (report.steps != null)
                {
                    foreach (var step in report.steps)
                    {
                        if (step.messages != null)
                        {
                            foreach (var message in step.messages)
                            {
                                if (message.type == LogType.Error || message.type == LogType.Exception)
                                {
                                    LogError($"Build Error: {message.content}");
                                }
                            }
                        }
                    }
                }
                
                LogError("========================================");
                EditorApplication.Exit(1);
            }
        }
        catch (Exception e)
        {
            LogError($"Exception during Android AAB build: {e.Message}");
            LogError(e.StackTrace);
            EditorApplication.Exit(1);
        }
    }

    // ============================================
    // iOS BUILD
    // ============================================
    
    /// <summary>
    /// Build iOS Xcode Project
    /// Usage: -executeMethod BuildScript.BuildiOS -buildPath "Builds/iOS" -versionNumber "1.0.0" -buildNumber "1"
    /// </summary>
    [MenuItem("Build/iOS Xcode Project")]
    public static void BuildiOS()
    {
        Log("========================================");
        Log("🍎 Building iOS Xcode Project...");
        Log("========================================");

        try
        {
            // Lấy tham số
            string baseBuildPath = GetArgument("buildPath") ?? "Builds/iOS";
            string versionNumber = GetArgument("versionNumber") ?? PlayerSettings.bundleVersion;
            string buildNumber = GetArgument("buildNumber") ?? PlayerSettings.iOS.buildNumber;

            // Cập nhật version
            PlayerSettings.bundleVersion = versionNumber;
            PlayerSettings.iOS.buildNumber = buildNumber;

            // Tạo thư mục theo version: Builds/iOS/1.0.0/
            string buildPath = Path.Combine(baseBuildPath, versionNumber);
            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }

            Log($"Build Path: {buildPath}");
            Log($"Version: {versionNumber}");
            Log($"Build Number: {buildNumber}");

            // iOS Settings
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.targetOSVersionString = "12.0"; // iOS minimum version
            
            // Lấy scenes
            string[] scenes = GetEnabledScenes();
            Log($"Scenes: {string.Join(", ", scenes)}");

            // Build options
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            Log("Building Xcode Project...");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            // Kiểm tra kết quả
            if (summary.result == BuildResult.Succeeded)
            {
                Log("========================================");
                Log($"✅ iOS Build SUCCEEDED!");
                Log($"Build time: {summary.totalTime}");
                Log($"Output: {buildPath}");
                Log("⚠️  Tiếp theo: Sử dụng Xcode để build IPA và deploy TestFlight");
                Log("========================================");
                EditorApplication.Exit(0);
            }
            else
            {
                LogError("========================================");
                LogError($"❌ iOS Build FAILED!");
                LogError($"Result: {summary.result}");
                LogError($"Errors: {summary.totalErrors}");
                LogError("========================================");
                EditorApplication.Exit(1);
            }
        }
        catch (Exception e)
        {
            LogError($"Exception during iOS build: {e.Message}");
            LogError(e.StackTrace);
            EditorApplication.Exit(1);
        }
    }

    // ============================================
    // HELPER METHODS
    // ============================================

    /// <summary>
    /// Switch build target sang Android và kiểm tra support
    /// </summary>
    private static bool SwitchToAndroidBuildTarget()
    {
        Log($"Current active build target: {EditorUserBuildSettings.activeBuildTarget}");
        
        // Kiểm tra Android build target có được support không
        bool isSupported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
        Log($"Android Build Target Supported: {isSupported}");
        
        if (!isSupported)
        {
            LogError("❌ Android Build Target is NOT supported!");
            LogError("Please install Android Build Support in Unity Hub:");
            LogError("  Unity Hub → Installs → Add Modules → Android Build Support");
            return false;
        }

        // Nếu đã là Android thì không cần switch
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            Log("✅ Build target is already Android");
            return true;
        }

        // Switch sang Android
        Log($"Switching build target from {EditorUserBuildSettings.activeBuildTarget} to Android...");
        bool switchResult = EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Android, 
            BuildTarget.Android
        );
        
        if (switchResult)
        {
            Log("✅ Successfully switched to Android build target");
            return true;
        }
        else
        {
            LogError("❌ Failed to switch to Android build target!");
            LogError("This may indicate Android Build Support is not properly installed.");
            return false;
        }
    }

    /// <summary>
    /// Setup Android build settings (keystore, version, etc.)
    /// </summary>
    private static void SetupAndroidBuild()
    {
        // Lấy tham số từ command line
        string keystorePath = GetArgument("keystorePath");
        string keystorePass = GetArgument("keystorePass");
        string keyaliasName = GetArgument("keyaliasName");
        string keyaliasPass = GetArgument("keyaliasPass");
        string versionNumber = GetArgument("versionNumber") ?? PlayerSettings.bundleVersion;
        string buildNumber = GetArgument("buildNumber") ?? PlayerSettings.Android.bundleVersionCode.ToString();

        Log($"Version: {versionNumber}");
        Log($"Build Number: {buildNumber}");

        // Cập nhật version
        PlayerSettings.bundleVersion = versionNumber;
        
        // Parse build number to int
        if (int.TryParse(buildNumber, out int buildNumberInt))
        {
            PlayerSettings.Android.bundleVersionCode = buildNumberInt;
        }

        // Android settings
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24; // Android 7.0
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33; // Android 13

        // Keystore configuration
        if (!string.IsNullOrEmpty(keystorePath))
        {
            Log($"Using keystore: {keystorePath}");
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasName = keyaliasName;
            PlayerSettings.Android.keyaliasPass = keyaliasPass;
        }
        else
        {
            LogError("⚠️  Warning: No keystore provided! Using debug keystore.");
        }

        // Scripting backend (IL2CPP for better performance)
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
    }

    /// <summary>
    /// Lấy danh sách scenes được enable trong Build Settings
    /// </summary>
    private static string[] GetEnabledScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
    }

    /// <summary>
    /// Format bytes thành string dễ đọc
    /// </summary>
    private static string FormatBytes(ulong bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        // Log raw value để debug
        Log($"Raw bytes value: {bytes}");
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        // Format với 2 số thập phân
        return $"{len:0.##} {sizes[order]}";
    }
    
    /// <summary>
    /// Lấy kích thước file thực tế từ disk (chính xác hơn)
    /// </summary>
    private static string GetActualFileSize(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return "N/A";
        }
        
        FileInfo fileInfo = new FileInfo(filePath);
        long bytes = fileInfo.Length;
        
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }

    // ============================================
    // MENU ITEMS - Build từ Unity Editor
    // ============================================

    [MenuItem("Build/Build All Platforms")]
    public static void BuildAllPlatforms()
    {
        Log("========================================");
        Log("🚀 Building All Platforms...");
        Log("========================================");

        BuildWindows();
        BuildAndroidAPK();
        BuildAndroidAAB();
        BuildiOS();

        Log("========================================");
        Log("✅ All Platforms Build Completed!");
        Log("========================================");
    }

    [MenuItem("Build/Clear Build Folder")]
    public static void ClearBuildFolder()
    {
        string buildPath = "Builds";
        if (Directory.Exists(buildPath))
        {
            Directory.Delete(buildPath, true);
            Log($"✅ Cleared build folder: {buildPath}");
        }
        else
        {
            Log($"⚠️  Build folder doesn't exist: {buildPath}");
        }
    }
}
