# ============================================
# Script Test Android Build Locally
# Trước khi chạy trên Jenkins
# ============================================

param(
    [string]$UnityVersion = "6000.2.6f2",
    [string]$ProjectPath = "$PSScriptRoot\..",
    [string]$BuildPath = "$PSScriptRoot\..\Builds\Android",
    [string]$VersionNumber = "1.0.0",
    [string]$BuildNumber = "999"
)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  TEST ANDROID BUILD LOCALLY" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# ============================================
# 1. Validate paths
# ============================================
Write-Host "[1/5] Kiểm tra đường dẫn..." -ForegroundColor Yellow

$UnityPath = "$env:ProgramFiles\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"

if (-not (Test-Path $UnityPath)) {
    Write-Host "❌ KHÔNG TÌM THẤY Unity $UnityVersion" -ForegroundColor Red
    Write-Host "Path: $UnityPath" -ForegroundColor Gray
    exit 1
}

Write-Host "✅ Unity found: $UnityVersion" -ForegroundColor Green

# Resolve full paths
$ProjectPath = Resolve-Path $ProjectPath
$BuildPath = if (Test-Path $BuildPath) { Resolve-Path $BuildPath } else { [System.IO.Path]::GetFullPath($BuildPath) }

Write-Host "✅ Project: $ProjectPath" -ForegroundColor Green
Write-Host "✅ Build output: $BuildPath" -ForegroundColor Green

# ============================================
# 2. Check Android Build Support
# ============================================
Write-Host ""
Write-Host "[2/5] Kiểm tra Android Build Support..." -ForegroundColor Yellow

$AndroidPlayerPath = "$env:ProgramFiles\Unity\Hub\Editor\$UnityVersion\Editor\Data\PlaybackEngines\AndroidPlayer"

if (-not (Test-Path $AndroidPlayerPath)) {
    Write-Host "❌ ANDROID BUILD SUPPORT CHƯA CÀI ĐẶT!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Hãy chạy script: Install-AndroidBuildSupport.ps1" -ForegroundColor Yellow
    Write-Host "Hoặc cài qua Unity Hub → Installs → Add Modules" -ForegroundColor Yellow
    exit 1
}

$hasSDK = Test-Path "$AndroidPlayerPath\SDK"
$hasNDK = Test-Path "$AndroidPlayerPath\NDK"
$hasJDK = Test-Path "$AndroidPlayerPath\OpenJDK"

Write-Host "  Android SDK: $(if($hasSDK){'✅'}else{'❌'})" -ForegroundColor $(if($hasSDK){'Green'}else{'Red'})
Write-Host "  Android NDK: $(if($hasNDK){'✅'}else{'❌'})" -ForegroundColor $(if($hasNDK){'Green'}else{'Red'})
Write-Host "  OpenJDK:     $(if($hasJDK){'✅'}else{'❌'})" -ForegroundColor $(if($hasJDK){'Green'}else{'Red'})

if (-not ($hasSDK -and $hasNDK -and $hasJDK)) {
    Write-Host ""
    Write-Host "❌ MỘT SỐ COMPONENTS THIẾU!" -ForegroundColor Red
    Write-Host "Hãy chạy script: Install-AndroidBuildSupport.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Tất cả components đã sẵn sàng" -ForegroundColor Green

# ============================================
# 3. Clean previous build
# ============================================
Write-Host ""
Write-Host "[3/5] Dọn dẹp build cũ..." -ForegroundColor Yellow

$OutputAPK = "$BuildPath\$VersionNumber\*.apk"

if (Test-Path $OutputAPK) {
    Write-Host "  Xóa build cũ: $OutputAPK" -ForegroundColor Gray
    Remove-Item -Path "$BuildPath\$VersionNumber" -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "✅ Sẵn sàng build mới" -ForegroundColor Green

# ============================================
# 4. Build Android APK
# ============================================
Write-Host ""
Write-Host "[4/5] Bắt đầu build Android APK..." -ForegroundColor Yellow
Write-Host ""
Write-Host "  Version: $VersionNumber" -ForegroundColor Cyan
Write-Host "  Build Number: $BuildNumber" -ForegroundColor Cyan
Write-Host "  Output: $BuildPath\$VersionNumber" -ForegroundColor Cyan
Write-Host ""
Write-Host "⏳ Quá trình này có thể mất 5-20 phút..." -ForegroundColor Yellow
Write-Host "⏳ Xem progress trong file: unity-test-build.log" -ForegroundColor Yellow
Write-Host ""

$LogFile = "$ProjectPath\unity-test-build.log"

# Remove old log
if (Test-Path $LogFile) {
    Remove-Item $LogFile -Force
}

# Build command
$arguments = @(
    "-quit",
    "-batchmode",
    "-buildTarget", "Android",
    "-projectPath", "`"$ProjectPath`"",
    "-executeMethod", "BuildScript.BuildAndroidAPK",
    "-buildPath", "`"$BuildPath`"",
    "-versionNumber", $VersionNumber,
    "-buildNumber", $BuildNumber,
    "-logFile", "`"$LogFile`""
)

Write-Host "Command:" -ForegroundColor Gray
Write-Host "  `"$UnityPath`" $($arguments -join ' ')" -ForegroundColor DarkGray
Write-Host ""

$buildStartTime = Get-Date

try {
    $process = Start-Process -FilePath $UnityPath `
        -ArgumentList $arguments `
        -Wait -PassThru -NoNewWindow
    
    $buildEndTime = Get-Date
    $buildDuration = $buildEndTime - $buildStartTime
    
    Write-Host ""
    Write-Host "Build completed in $([math]::Round($buildDuration.TotalMinutes, 2)) minutes" -ForegroundColor Cyan
    Write-Host ""
    
    if ($process.ExitCode -eq 0) {
        Write-Host "✅ BUILD THÀNH CÔNG!" -ForegroundColor Green
    } else {
        Write-Host "❌ BUILD THẤT BẠI (Exit Code: $($process.ExitCode))" -ForegroundColor Red
        Write-Host ""
        Write-Host "Xem log chi tiết tại: $LogFile" -ForegroundColor Yellow
        
        # Show last 50 lines of log
        if (Test-Path $LogFile) {
            Write-Host ""
            Write-Host "=== LAST 50 LINES OF LOG ===" -ForegroundColor Yellow
            Get-Content $LogFile -Tail 50
        }
        
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "❌ LỖI KHI CHẠY UNITY: $_" -ForegroundColor Red
    exit 1
}

# ============================================
# 5. Verify output
# ============================================
Write-Host ""
Write-Host "[5/5] Xác nhận output..." -ForegroundColor Yellow

$apkFiles = Get-ChildItem -Path "$BuildPath\$VersionNumber" -Filter "*.apk" -ErrorAction SilentlyContinue

if ($apkFiles) {
    Write-Host ""
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host "  BUILD THÀNH CÔNG! 🎉" -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host ""
    
    foreach ($apk in $apkFiles) {
        $sizeMB = [math]::Round($apk.Length / 1MB, 2)
        Write-Host "APK File:" -ForegroundColor Cyan
        Write-Host "  Path: $($apk.FullName)" -ForegroundColor Gray
        Write-Host "  Size: $sizeMB MB" -ForegroundColor Gray
        Write-Host ""
    }
    
    Write-Host "Tiếp theo:" -ForegroundColor Cyan
    Write-Host "  1. Cài APK lên thiết bị Android để test" -ForegroundColor Gray
    Write-Host "  2. Hoặc chạy Jenkins build với cùng cấu hình" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Để cài APK:" -ForegroundColor Yellow
    Write-Host "  adb install -r `"$($apkFiles[0].FullName)`"" -ForegroundColor DarkGray
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "❌ KHÔNG TÌM THẤY FILE APK!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Build có thể đã thất bại. Kiểm tra log:" -ForegroundColor Yellow
    Write-Host "  $LogFile" -ForegroundColor Gray
    Write-Host ""
    
    # Show errors from log
    if (Test-Path $LogFile) {
        Write-Host "=== ERRORS FROM LOG ===" -ForegroundColor Yellow
        Select-String -Path $LogFile -Pattern "error|exception|failed" -CaseSensitive:$false -Context 2 | Select-Object -First 20
    }
    
    exit 1
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Show full log location
Write-Host "Full build log: $LogFile" -ForegroundColor Gray
Write-Host ""

exit 0

