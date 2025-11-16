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
    // Jenkins Password: kdrpppnoxvsrload
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
        bool success = BuildWindowsInternal();
        
        // Chỉ exit khi chạy từ command line (batch mode hoặc bị gọi trực tiếp)
        if (ShouldExitAfterBuild())
        {
            EditorApplication.Exit(success ? 0 : 1);
        }
    }

    /// <summary>
    /// Internal method để build Windows - không tự động exit
    /// Trả về true nếu build thành công, false nếu thất bại
    /// </summary>
    private static bool BuildWindowsInternal()
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
                return true;
            }
            else
            {
                LogError("========================================");
                LogError($"❌ Windows Build FAILED!");
                LogError($"Result: {summary.result}");
                LogError($"Errors: {summary.totalErrors}");
                LogError("========================================");
                return false;
            }
        }
        catch (Exception e)
        {
            LogError($"Exception during Windows build: {e.Message}");
            LogError(e.StackTrace);
            return false;
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
        bool success = BuildAndroidAPKInternal();
        
        // Chỉ exit khi chạy từ command line (batch mode hoặc bị gọi trực tiếp)
        if (ShouldExitAfterBuild())
        {
            EditorApplication.Exit(success ? 0 : 1);
        }
    }

    /// <summary>
    /// Internal method để build APK - không tự động exit
    /// Trả về true nếu build thành công, false nếu thất bại
    /// </summary>
    private static bool BuildAndroidAPKInternal()
    {
        Log("========================================");
        Log("🤖 Building Android APK...");
        Log("========================================");

        try
        {
            // Trong batch mode, Unity đã được khởi động với -buildTarget Android
            // Không cần switch target tường minh - BuildPlayerOptions sẽ xử lý
            bool isBatchMode = IsBatchMode();
            if (isBatchMode)
            {
                Log("Step 1: Batch mode detected - skipping explicit target switch");
                Log($"Active build target (info only): {EditorUserBuildSettings.activeBuildTarget}");
                Log("✅ Unity was started with -buildTarget Android - BuildPlayerOptions will handle the target");
            }
            else
            {
                // Editor mode - vẫn cần switch target
                Log("Step 1: Checking and switching build target...");
                if (!SwitchToAndroidBuildTarget())
                {
                    LogError("========================================");
                    LogError("❌ Cannot proceed with Android build!");
                    LogError("Build target switch failed.");
                    LogError("========================================");
                    return false;
                }
                Log("✅ Build target check completed");
            }

            // Setup Android build
            Log("Step 2: Setting up Android build configuration...");
            SetupAndroidBuild();
            Log("✅ Android build configuration completed");

            // Build APK (không phải AAB)
            Log("Step 3: Configuring build type (APK)...");
            EditorUserBuildSettings.buildAppBundle = false;
            Log("✅ Build type set to APK");

            // Lấy base path và version
            Log("Step 4: Preparing build paths...");
            string baseBuildPath = GetArgument("buildPath") ?? "Builds/Android";
            string versionNumber = GetArgument("versionNumber") ?? PlayerSettings.bundleVersion;
            string productName = PlayerSettings.productName;
            
            // Tạo path theo version: Builds/Android/1.0.0/game.apk
            string versionPath = Path.Combine(baseBuildPath, versionNumber);
            if (!Directory.Exists(versionPath))
            {
                Directory.CreateDirectory(versionPath);
                Log($"Created directory: {versionPath}");
            }
            
            string buildPath = Path.Combine(versionPath, $"{productName}.apk");
            Log($"Build Path: {buildPath}");
            Log("✅ Build paths prepared");

            // Kiểm tra scenes
            Log("Step 5: Validating scenes...");
            string[] scenes = GetEnabledScenes();
            if (scenes == null || scenes.Length == 0)
            {
                LogError("========================================");
                LogError("❌ No scenes enabled in Build Settings!");
                LogError("Please add at least one scene to Build Settings:");
                LogError("  File → Build Settings → Add Open Scenes");
                LogError("========================================");
                return false;
            }
            
            Log($"✅ Found {scenes.Length} scene(s) to build:");
            for (int i = 0; i < scenes.Length; i++)
            {
                Log($"  [{i + 1}] {scenes[i]}");
            }

            Log("Step 6: Verifying Android build target is supported...");
            bool isAndroidSupported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
            if (!isAndroidSupported)
            {
                LogError("========================================");
                LogError("❌ Android build target is NOT SUPPORTED!");
                LogError("");
                LogError("This means Unity cannot find Android Build Support module.");
                LogError("");
                LogError("Please verify:");
                LogError("  1. Android Build Support is installed for Unity " + Application.unityVersion);
                LogError("  2. Check: C:\\Program Files\\Unity\\Hub\\Editor\\" + Application.unityVersion + "\\Editor\\Data\\PlaybackEngines\\AndroidPlayer");
                LogError("  3. Restart Unity/Jenkins after installing Android modules");
                LogError("  4. Make sure Unity was started with -buildTarget Android parameter");
                LogError("========================================");
                return false;
            }
            Log("✅ Android build target is supported");
            
            Log("Step 7: Creating build options...");
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            
            // Verify build target one more time
            Log($"Final verification - Active build target: {EditorUserBuildSettings.activeBuildTarget}");
            Log($"Final verification - Target in options: {buildPlayerOptions.target}");
            Log($"Final verification - Android supported: {isAndroidSupported}");
            Log("✅ Build options created");

            Log("Step 8: Starting APK build process...");
            Log("This may take several minutes...");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;
            Log("✅ Build process completed");

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
                return true;
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
                return false;
            }
        }
        catch (Exception e)
        {
            LogError($"Exception during Android APK build: {e.Message}");
            LogError(e.StackTrace);
            return false;
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
        bool success = BuildAndroidAABInternal();
        
        // Chỉ exit khi chạy từ command line (batch mode hoặc bị gọi trực tiếp)
        if (ShouldExitAfterBuild())
        {
            EditorApplication.Exit(success ? 0 : 1);
        }
    }

    /// <summary>
    /// Internal method để build AAB - không tự động exit
    /// Trả về true nếu build thành công, false nếu thất bại
    /// </summary>
    private static bool BuildAndroidAABInternal()
    {
        Log("========================================");
        Log("📦 Building Android App Bundle (AAB)...");
        Log("========================================");

        try
        {
            // Trong batch mode, Unity đã được khởi động với -buildTarget Android
            // Không cần switch target tường minh - BuildPlayerOptions sẽ xử lý
            bool isBatchMode = IsBatchMode();
            if (isBatchMode)
            {
                Log("Step 1: Batch mode detected - skipping explicit target switch");
                Log($"Active build target (info only): {EditorUserBuildSettings.activeBuildTarget}");
                Log("✅ Unity was started with -buildTarget Android - BuildPlayerOptions will handle the target");
            }
            else
            {
                // Editor mode - vẫn cần switch target
                Log("Step 1: Checking and switching build target...");
                if (!SwitchToAndroidBuildTarget())
                {
                    LogError("========================================");
                    LogError("❌ Cannot proceed with Android build!");
                    LogError("Build target switch failed.");
                    LogError("========================================");
                    return false;
                }
                Log("✅ Build target check completed");
            }

            // Setup Android build
            Log("Step 2: Setting up Android build configuration...");
            SetupAndroidBuild();
            Log("✅ Android build configuration completed");

            // Build AAB
            Log("Step 3: Configuring build type (AAB)...");
            EditorUserBuildSettings.buildAppBundle = true;
            Log("✅ Build type set to AAB");

            // Lấy base path và version
            Log("Step 4: Preparing build paths...");
            string baseBuildPath = GetArgument("buildPath") ?? "Builds/Android";
            string versionNumber = GetArgument("versionNumber") ?? PlayerSettings.bundleVersion;
            string productName = PlayerSettings.productName;
            
            // Tạo path theo version: Builds/Android/1.0.0/game.aab
            string versionPath = Path.Combine(baseBuildPath, versionNumber);
            if (!Directory.Exists(versionPath))
            {
                Directory.CreateDirectory(versionPath);
                Log($"Created directory: {versionPath}");
            }
            
            string buildPath = Path.Combine(versionPath, $"{productName}.aab");
            Log($"Build Path: {buildPath}");
            Log("✅ Build paths prepared");

            // Kiểm tra scenes
            Log("Step 5: Validating scenes...");
            string[] scenes = GetEnabledScenes();
            if (scenes == null || scenes.Length == 0)
            {
                LogError("========================================");
                LogError("❌ No scenes enabled in Build Settings!");
                LogError("Please add at least one scene to Build Settings:");
                LogError("  File → Build Settings → Add Open Scenes");
                LogError("========================================");
                return false;
            }
            
            Log($"✅ Found {scenes.Length} scene(s) to build:");
            for (int i = 0; i < scenes.Length; i++)
            {
                Log($"  [{i + 1}] {scenes[i]}");
            }

            Log("Step 6: Verifying Android build target is supported...");
            bool isAndroidSupported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
            if (!isAndroidSupported)
            {
                LogError("========================================");
                LogError("❌ Android build target is NOT SUPPORTED!");
                LogError("");
                LogError("This means Unity cannot find Android Build Support module.");
                LogError("");
                LogError("Please verify:");
                LogError("  1. Android Build Support is installed for Unity " + Application.unityVersion);
                LogError("  2. Check: C:\\Program Files\\Unity\\Hub\\Editor\\" + Application.unityVersion + "\\Editor\\Data\\PlaybackEngines\\AndroidPlayer");
                LogError("  3. Restart Unity/Jenkins after installing Android modules");
                LogError("  4. Make sure Unity was started with -buildTarget Android parameter");
                LogError("========================================");
                return false;
            }
            Log("✅ Android build target is supported");
            
            Log("Step 7: Creating build options...");
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            
            // Verify build target one more time
            Log($"Final verification - Active build target: {EditorUserBuildSettings.activeBuildTarget}");
            Log($"Final verification - Target in options: {buildPlayerOptions.target}");
            Log($"Final verification - Android supported: {isAndroidSupported}");
            Log("✅ Build options created");

            Log("Step 8: Starting AAB build process...");
            Log("This may take several minutes...");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;
            Log("✅ Build process completed");

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
                return true;
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
                return false;
            }
        }
        catch (Exception e)
        {
            LogError($"Exception during Android AAB build: {e.Message}");
            LogError(e.StackTrace);
            return false;
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
        bool success = BuildiOSInternal();
        
        // Chỉ exit khi chạy từ command line (batch mode hoặc bị gọi trực tiếp)
        if (ShouldExitAfterBuild())
        {
            EditorApplication.Exit(success ? 0 : 1);
        }
    }

    /// <summary>
    /// Internal method để build iOS - không tự động exit
    /// Trả về true nếu build thành công, false nếu thất bại
    /// </summary>
    private static bool BuildiOSInternal()
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
            PlayerSettings.iOS.targetOSVersionString = "15.0"; // iOS minimum version
            
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
                return true;
            }
            else
            {
                LogError("========================================");
                LogError($"❌ iOS Build FAILED!");
                LogError($"Result: {summary.result}");
                LogError($"Errors: {summary.totalErrors}");
                LogError("========================================");
                return false;
            }
        }
        catch (Exception e)
        {
            LogError($"Exception during iOS build: {e.Message}");
            LogError(e.StackTrace);
            return false;
        }
    }

    // ============================================
    // HELPER METHODS
    // ============================================

    /// <summary>
    /// Kiểm tra xem Unity đang chạy trong batch mode hay không
    /// </summary>
    private static bool IsBatchMode()
    {
        string[] args = Environment.GetCommandLineArgs();
        return args.Contains("-batchmode") || args.Contains("-quit");
    }

    /// <summary>
    /// Kiểm tra xem có nên exit sau khi build hay không
    /// Chỉ exit khi chạy từ command line với -executeMethod
    /// Không exit khi build từ menu hoặc từ BuildAllPlatforms
    /// </summary>
    private static bool ShouldExitAfterBuild()
    {
        string[] args = Environment.GetCommandLineArgs();
        
        // Nếu có -executeMethod trong command line, có nghĩa là được gọi trực tiếp từ Jenkins/CI
        // và cần exit để trả về exit code
        bool hasExecuteMethod = args.Contains("-executeMethod");
        
        // Nếu có -batchmode, đây là batch mode từ Jenkins/CI
        bool isBatchMode = IsBatchMode();
        
        // Exit khi chạy từ command line với executeMethod
        return hasExecuteMethod || isBatchMode;
    }

    /// <summary>
    /// Switch build target sang Android
    /// Xử lý khác nhau cho batch mode (Jenkins/CI) và editor mode
    /// </summary>
    private static bool SwitchToAndroidBuildTarget()
    {
        try
        {
            bool isBatchMode = IsBatchMode();
            Log($"Running mode: {(isBatchMode ? "Batch Mode (CI/Jenkins)" : "Unity Editor Mode")}");
            
            BuildTarget currentTarget = EditorUserBuildSettings.activeBuildTarget;
            Log($"Current active build target: {currentTarget}");

            // Nếu đã là Android thì không cần switch
            if (currentTarget == BuildTarget.Android)
            {
                Log("✅ Build target is already Android");
                return true;
            }

            // Switch sang Android
            Log($"Attempting to switch build target from {currentTarget} to Android...");
            
            try
            {
                bool switchResult = EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android, 
                    BuildTarget.Android
                );
                
                Log($"SwitchActiveBuildTarget returned: {switchResult}");
                
                // Trong batch mode, switch có thể fail nhưng BuildPlayerOptions vẫn hoạt động
                if (isBatchMode)
                {
                    if (switchResult)
                    {
                        BuildTarget newTarget = EditorUserBuildSettings.activeBuildTarget;
                        Log($"Active build target after switch: {newTarget}");
                        if (newTarget == BuildTarget.Android)
                        {
                            Log("✅ Successfully switched to Android in batch mode (verified)");
                        }
                        else
                        {
                            Log($"⚠️  Active target is still {newTarget}, but continuing anyway");
                            Log("⚠️  BuildPlayerOptions will handle the target switch during build");
                        }
                        return true;
                    }
                    else
                    {
                        // Switch failed trong batch mode - ĐÂY LÀ BÌNH THƯỜNG trong batch mode
                        // Unity trong batch mode thường không cho phép switch target tường minh
                        // Nhưng BuildPlayerOptions.target = BuildTarget.Android sẽ tự động xử lý switch trong quá trình build
                        Log("⚠️  SwitchActiveBuildTarget returned FALSE in batch mode");
                        Log("⚠️  This is NORMAL behavior in batch mode - Unity doesn't allow explicit target switching");
                        Log("⚠️  BuildPlayerOptions.target = BuildTarget.Android will handle the switch during build");
                        Log("✅ Continuing with build - BuildPlayerOptions will switch target automatically");
                        return true; // Return true để tiếp tục build
                    }
                }
                else
                {
                    // Editor mode - cần switch thành công
                    if (switchResult)
                    {
                        BuildTarget newTarget = EditorUserBuildSettings.activeBuildTarget;
                        if (newTarget == BuildTarget.Android)
                        {
                            Log("✅ Successfully switched to Android build target (verified)");
                            return true;
                        }
                        else
                        {
                            LogError($"⚠️  Switch reported success but active target is: {newTarget}");
                            return false;
                        }
                    }
                    else
                    {
                        LogError("========================================");
                        LogError("❌ Failed to switch to Android build target!");
                        LogError("");
                        LogError("Possible causes:");
                        LogError("  1. Android Build Support is not installed");
                        LogError("  2. Android SDK/NDK not configured");
                        LogError("");
                        LogError("Please check:");
                        LogError("  - Unity Hub → Installs → Add Modules → Android Build Support");
                        LogError("  - Edit → Preferences → External Tools → Android SDK/NDK paths");
                        LogError("========================================");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                if (isBatchMode)
                {
                    // Batch mode: log warning nhưng tiếp tục
                    Log("⚠️  Exception while switching in batch mode:");
                    Log($"   {ex.Message}");
                    Log("⚠️  Continuing anyway - BuildPlayerOptions will handle the switch");
                    return true;
                }
                else
                {
                    // Editor mode: báo lỗi và dừng
                    LogError("========================================");
                    LogError($"❌ Exception while switching build target: {ex.Message}");
                    LogError($"Type: {ex.GetType().Name}");
                    LogError($"Stack: {ex.StackTrace}");
                    LogError("========================================");
                    return false;
                }
            }
        }
        catch (Exception e)
        {
            bool isBatchMode = IsBatchMode();
            
            if (isBatchMode)
            {
                Log("⚠️  Exception in SwitchToAndroidBuildTarget (batch mode):");
                Log($"   {e.Message}");
                Log("⚠️  Continuing - BuildPlayerOptions will try to switch");
                return true;
            }
            else
            {
                LogError("========================================");
                LogError($"❌ Exception in SwitchToAndroidBuildTarget: {e.Message}");
                LogError($"Type: {e.GetType().Name}");
                LogError("========================================");
                return false;
            }
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
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35; // Android 13

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

        int successCount = 0;
        int failCount = 0;

        // Build Windows
        Log("\n[1/4] Building Windows...");
        if (BuildWindowsInternal())
        {
            successCount++;
            Log("✅ Windows build completed successfully");
        }
        else
        {
            failCount++;
            LogError("❌ Windows build failed");
        }

        // Build Android APK
        Log("\n[2/4] Building Android APK...");
        if (BuildAndroidAPKInternal())
        {
            successCount++;
            Log("✅ Android APK build completed successfully");
        }
        else
        {
            failCount++;
            LogError("❌ Android APK build failed");
        }

        // Build Android AAB
        Log("\n[3/4] Building Android AAB...");
        if (BuildAndroidAABInternal())
        {
            successCount++;
            Log("✅ Android AAB build completed successfully");
        }
        else
        {
            failCount++;
            LogError("❌ Android AAB build failed");
        }

        // Build iOS
        Log("\n[4/4] Building iOS...");
        if (BuildiOSInternal())
        {
            successCount++;
            Log("✅ iOS build completed successfully");
        }
        else
        {
            failCount++;
            LogError("❌ iOS build failed");
        }

        // Summary
        Log("========================================");
        Log("🎯 Build All Platforms Summary:");
        Log($"   ✅ Success: {successCount}");
        Log($"   ❌ Failed: {failCount}");
        Log($"   Total: {successCount + failCount}");
        Log("========================================");

        // Chỉ exit nếu được gọi từ command line
        if (ShouldExitAfterBuild())
        {
            EditorApplication.Exit(failCount > 0 ? 1 : 0);
        }
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
