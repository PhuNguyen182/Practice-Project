# 🔧 Hướng Dẫn Khắc Phục Lỗi Android Build

## ❌ Lỗi Gặp Phải
```
Build Error: Error building player because build target was unsupported
```

---

## 🎯 Nguyên Nhân Chính

Lỗi này xảy ra vì **Android Build Support chưa được cài đặt** cho Unity Editor trên máy Jenkins của bạn.

### Chi Tiết Kỹ Thuật:

1. **Vấn đề trong BuildScript.cs (ĐÃ FIX):**
   - Code cũ: Khi `SwitchActiveBuildTarget()` trả về `false`, code vẫn `return true` và tiếp tục build
   - Kết quả: Unity cố build Android mà không có module Android → Lỗi "build target was unsupported"
   - **✅ Đã sửa:** Giờ sẽ `return false` và hiển thị thông báo lỗi rõ ràng

2. **Vấn đề trong Jenkinsfile.groovy (ĐÃ FIX):**
   - Code cũ: Thiếu parameter `-buildTarget Android` khi gọi Unity
   - Kết quả: Unity không biết cần load Android module → Switch target thất bại
   - **✅ Đã sửa:** Thêm `-buildTarget Android` vào command line

---

## 🛠️ Cách Khắc Phục

### ✅ Bước 1: Cài Đặt Android Build Support

#### **Option A: Qua Unity Hub (Khuyến Nghị)**

1. Mở **Unity Hub**
2. Đi tới tab **Installs**
3. Tìm Unity version `6000.2.6f2` (hoặc version bạn đang dùng)
4. Click vào **icon bánh răng** (⚙️) bên cạnh version → chọn **Add Modules**
5. Tích chọn các module sau:
   ```
   ✓ Android Build Support
   ✓ Android SDK & NDK Tools
   ✓ OpenJDK
   ```
6. Click **Done** và chờ cài đặt hoàn tất (có thể mất 5-15 phút)

#### **Option B: Qua Command Line (Cho CI/CD)**

```bash
# Windows
"%ProgramFiles%\Unity Hub\Unity Hub.exe" -- --headless install-modules --version 6000.2.6f2 --module android android-sdk-ndk-tools android-open-jdk

# macOS/Linux
"/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless install-modules --version 6000.2.6f2 --module android android-sdk-ndk-tools android-open-jdk
```

---

### ✅ Bước 2: Kiểm Tra Android SDK/NDK Paths

1. Mở Unity Editor
2. Đi tới **Edit → Preferences → External Tools**
3. Kiểm tra các đường dẫn sau đã được thiết lập:
   - **Android SDK Path**: `C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK`
   - **Android NDK Path**: `C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Data\PlaybackEngines\AndroidPlayer\NDK`
   - **JDK Path**: `C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK`

4. Nếu các path chưa đúng, click **Download** để Unity tự động tải về

---

### ✅ Bước 3: Kiểm Tra Cài Đặt

Chạy lệnh sau để kiểm tra Android Build Support đã được cài đặt:

```bash
# Windows
cd "C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Data\PlaybackEngines"
dir

# Phải có thư mục "AndroidPlayer"
```

---

### ✅ Bước 4: Chạy Lại Build Trên Jenkins

Sau khi cài đặt xong, chạy lại Jenkins build:

1. Đi tới Jenkins job của bạn
2. Click **Build with Parameters**
3. Chọn build options:
   - **BUILD_TARGET**: `Android`
   - **BUILD_APK**: ✓
4. Click **Build**

Lần này build sẽ thành công! 🎉

---

## 📋 Code Changes Summary

### 1. BuildScript.cs

**ĐÃ THAY ĐỔI:**
```csharp
// CŨ (Line 685-691):
else {
    Log("⚠️  SwitchActiveBuildTarget returned false...");
    return true; // ❌ VẪN TIẾP TỤC → LỖI!
}

// MỚI:
else {
    LogError("❌ SwitchActiveBuildTarget returned FALSE...");
    LogError("Android Build Support CHƯA được cài đặt...");
    // ... hướng dẫn chi tiết ...
    return false; // ✅ DỪNG NGAY LẬP TỨC
}
```

### 2. Jenkinsfile.groovy

**ĐÃ THÊM:**
```groovy
bat """
    "${UNITY_PATH}" -quit -batchmode ^
    -buildTarget Android ^          # ✅ THÊM DÒNG NÀY
    -projectPath "${PROJECT_PATH}" ^
    -executeMethod BuildScript.BuildAndroidAPK ^
    ...
"""
```

---

## 🧪 Test Build Locally

Trước khi chạy trên Jenkins, bạn có thể test local:

```bash
# Windows Command Prompt
cd "E:\Sample Projects\Git Practicing\Practice-Project"

"C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Unity.exe" ^
  -quit -batchmode ^
  -buildTarget Android ^
  -projectPath "%CD%" ^
  -executeMethod BuildScript.BuildAndroidAPK ^
  -buildPath "Builds\Android" ^
  -versionNumber "1.0.0" ^
  -buildNumber "1" ^
  -logFile "unity-test-build.log"

# Kiểm tra log
type unity-test-build.log
```

Nếu thấy:
- ✅ `"✅ Android APK Build SUCCEEDED!"` → Thành công!
- ❌ `"❌ SwitchActiveBuildTarget returned FALSE"` → Cần cài Android Build Support

---

## 🔍 Troubleshooting

### Vấn Đề 1: Unity Hub không tìm thấy Android modules

**Giải pháp:**
```bash
# Cài thủ công từ Unity Archive
# 1. Tải Unity 6000.2.6f2 + Android Support từ: https://unity.com/releases/editor/archive
# 2. Cài đặt với checkbox "Android Build Support" được chọn
```

### Vấn Đề 2: Jenkins không có quyền truy cập Unity Hub

**Giải pháp:**
1. Chạy Jenkins service với user account (không phải SYSTEM)
2. Hoặc cài Unity + Android modules cho SYSTEM account

### Vấn Đề 3: "Android SDK not found"

**Giải pháp:**
```bash
# Tải Android SDK riêng từ Android Studio
# Hoặc dùng Unity's built-in SDK:
# Edit → Preferences → External Tools → Android SDK/NDK → Use Embedded
```

---

## 📞 Liên Hệ Hỗ Trợ

Nếu vẫn gặp vấn đề, cung cấp thông tin sau:

1. **Unity Version**: `6000.2.6f2`
2. **Jenkins Log**: File `unity-build-apk.log`
3. **Unity Editor Log**: `Editor.log` từ Unity
4. **Installed Modules**: Screenshot từ Unity Hub → Installs

---

## ✅ Checklist Hoàn Thành

- [ ] Cài đặt Android Build Support cho Unity 6000.2.6f2
- [ ] Cài đặt Android SDK & NDK Tools
- [ ] Cài đặt OpenJDK
- [ ] Verify paths trong Unity → Edit → Preferences → External Tools
- [ ] Test build local thành công
- [ ] Chạy lại Jenkins build thành công
- [ ] Verify file APK được tạo tại `Builds/Android/1.0.0/[ProductName].apk`

---

**🎉 Sau khi hoàn thành checklist trên, Android build sẽ chạy hoàn hảo!**

