# ============================================
# Script Tự Động Cài Đặt Android Build Support
# Dành cho Unity 6000.2.6f2 trên Jenkins/CI
# ============================================

param(
    [string]$UnityVersion = "6000.2.6f2",
    [string]$UnityHubPath = "$env:ProgramFiles\Unity Hub\Unity Hub.exe"
)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  UNITY ANDROID BUILD SUPPORT INSTALLER" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# ============================================
# 1. Kiểm tra Unity Hub
# ============================================
Write-Host "[1/5] Kiểm tra Unity Hub..." -ForegroundColor Yellow

if (-not (Test-Path $UnityHubPath)) {
    Write-Host "❌ KHÔNG TÌM THẤY Unity Hub tại: $UnityHubPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Vui lòng cài đặt Unity Hub từ: https://unity.com/download" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Unity Hub tìm thấy: $UnityHubPath" -ForegroundColor Green

# ============================================
# 2. Kiểm tra Unity Editor
# ============================================
Write-Host ""
Write-Host "[2/5] Kiểm tra Unity Editor $UnityVersion..." -ForegroundColor Yellow

$UnityEditorPath = "$env:ProgramFiles\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"

if (-not (Test-Path $UnityEditorPath)) {
    Write-Host "❌ KHÔNG TÌM THẤY Unity $UnityVersion" -ForegroundColor Red
    Write-Host ""
    Write-Host "Vui lòng cài đặt Unity $UnityVersion qua Unity Hub trước" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Unity Editor tìm thấy: $UnityEditorPath" -ForegroundColor Green

# ============================================
# 3. Kiểm tra Android Build Support hiện tại
# ============================================
Write-Host ""
Write-Host "[3/5] Kiểm tra Android Build Support hiện có..." -ForegroundColor Yellow

$AndroidPlayerPath = "$env:ProgramFiles\Unity\Hub\Editor\$UnityVersion\Editor\Data\PlaybackEngines\AndroidPlayer"

if (Test-Path $AndroidPlayerPath) {
    Write-Host "✅ Android Build Support ĐÃ được cài đặt" -ForegroundColor Green
    Write-Host ""
    Write-Host "Thư mục: $AndroidPlayerPath" -ForegroundColor Gray
    
    # Kiểm tra các components
    $hasSDK = Test-Path "$AndroidPlayerPath\SDK"
    $hasNDK = Test-Path "$AndroidPlayerPath\NDK"
    $hasJDK = Test-Path "$AndroidPlayerPath\OpenJDK"
    
    Write-Host ""
    Write-Host "Components:" -ForegroundColor Cyan
    Write-Host "  Android SDK: $(if($hasSDK){'✅'}else{'❌'})" -ForegroundColor $(if($hasSDK){'Green'}else{'Red'})
    Write-Host "  Android NDK: $(if($hasNDK){'✅'}else{'❌'})" -ForegroundColor $(if($hasNDK){'Green'}else{'Red'})
    Write-Host "  OpenJDK:     $(if($hasJDK){'✅'}else{'❌'})" -ForegroundColor $(if($hasJDK){'Green'}else{'Red'})
    
    if ($hasSDK -and $hasNDK -and $hasJDK) {
        Write-Host ""
        Write-Host "✅ TẤT CẢ COMPONENTS ĐÃ CÀI ĐẶT HOÀN CHỈNH" -ForegroundColor Green
        Write-Host ""
        Write-Host "Bạn có thể build Android ngay bây giờ!" -ForegroundColor Cyan
        exit 0
    } else {
        Write-Host ""
        Write-Host "⚠️  MỘT SỐ COMPONENTS THIẾU - Tiếp tục cài đặt..." -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠️  Android Build Support CHƯA được cài đặt" -ForegroundColor Yellow
}

# ============================================
# 4. Cài đặt Android Build Support
# ============================================
Write-Host ""
Write-Host "[4/5] Bắt đầu cài đặt Android Build Support..." -ForegroundColor Yellow
Write-Host ""
Write-Host "⏳ Quá trình này có thể mất 5-15 phút..." -ForegroundColor Cyan
Write-Host "⏳ Vui lòng KHÔNG đóng cửa sổ này!" -ForegroundColor Cyan
Write-Host ""

# Tạo command để cài modules
$modules = @(
    "android",
    "android-sdk-ndk-tools", 
    "android-open-jdk"
)

$moduleArgs = $modules -join " "

try {
    # Chạy Unity Hub CLI để cài modules
    $command = "& `"$UnityHubPath`" -- --headless install-modules --version $UnityVersion --module $moduleArgs"
    
    Write-Host "Đang chạy lệnh:" -ForegroundColor Gray
    Write-Host $command -ForegroundColor DarkGray
    Write-Host ""
    
    $process = Start-Process -FilePath $UnityHubPath `
        -ArgumentList "-- --headless install-modules --version $UnityVersion --module $moduleArgs" `
        -Wait -PassThru -NoNewWindow
    
    if ($process.ExitCode -eq 0) {
        Write-Host ""
        Write-Host "✅ CÀI ĐẶT THÀNH CÔNG!" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "❌ CÀI ĐẶT THẤT BẠI (Exit Code: $($process.ExitCode))" -ForegroundColor Red
        Write-Host ""
        Write-Host "Hãy thử cài thủ công qua Unity Hub UI:" -ForegroundColor Yellow
        Write-Host "  1. Mở Unity Hub" -ForegroundColor Gray
        Write-Host "  2. Installs → $UnityVersion → ⚙️ → Add Modules" -ForegroundColor Gray
        Write-Host "  3. Chọn: Android Build Support (tất cả sub-modules)" -ForegroundColor Gray
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "❌ LỖI KHI CHẠY UNITY HUB CLI: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Hãy thử cài thủ công qua Unity Hub UI" -ForegroundColor Yellow
    exit 1
}

# ============================================
# 5. Verify cài đặt
# ============================================
Write-Host ""
Write-Host "[5/5] Xác nhận cài đặt..." -ForegroundColor Yellow

Start-Sleep -Seconds 2  # Đợi file system sync

$AndroidPlayerPath = "$env:ProgramFiles\Unity\Hub\Editor\$UnityVersion\Editor\Data\PlaybackEngines\AndroidPlayer"

if (Test-Path $AndroidPlayerPath) {
    $hasSDK = Test-Path "$AndroidPlayerPath\SDK"
    $hasNDK = Test-Path "$AndroidPlayerPath\NDK"
    $hasJDK = Test-Path "$AndroidPlayerPath\OpenJDK"
    
    Write-Host ""
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host "  CÀI ĐẶT HOÀN TẤT!" -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Components đã cài:" -ForegroundColor Cyan
    Write-Host "  ✅ Android Build Support" -ForegroundColor Green
    Write-Host "  $(if($hasSDK){'✅'}else{'❌'}) Android SDK & NDK Tools" -ForegroundColor $(if($hasSDK){'Green'}else{'Red'})
    Write-Host "  $(if($hasJDK){'✅'}else{'❌'}) OpenJDK" -ForegroundColor $(if($hasJDK){'Green'}else{'Red'})
    Write-Host ""
    Write-Host "Đường dẫn:" -ForegroundColor Cyan
    Write-Host "  $AndroidPlayerPath" -ForegroundColor Gray
    Write-Host ""
    
    if ($hasSDK -and $hasNDK -and $hasJDK) {
        Write-Host "🎉 BẠN ĐÃ SẴN SÀNG BUILD ANDROID!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Tiếp theo:" -ForegroundColor Cyan
        Write-Host "  1. Chạy lại Jenkins build job" -ForegroundColor Gray
        Write-Host "  2. Hoặc test local bằng script Test-AndroidBuild.ps1" -ForegroundColor Gray
        Write-Host ""
    } else {
        Write-Host "⚠️  MỘT SỐ COMPONENTS VẪN THIẾU" -ForegroundColor Yellow
        Write-Host "Hãy cài thủ công qua Unity Hub" -ForegroundColor Yellow
    }
} else {
    Write-Host ""
    Write-Host "❌ KHÔNG THỂ XÁC NHẬN CÀI ĐẶT" -ForegroundColor Red
    Write-Host "Thư mục AndroidPlayer không tồn tại" -ForegroundColor Red
    exit 1
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

exit 0

