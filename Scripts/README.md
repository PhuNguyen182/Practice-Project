# 🔧 Unity Build Scripts

Collection of PowerShell scripts để hỗ trợ Unity build automation trên Jenkins.

---

## 📁 Files Trong Thư Mục

### 1. `Install-AndroidBuildSupport.ps1`
**Mục đích:** Tự động cài đặt Android Build Support cho Unity Editor

**Sử dụng:**
```powershell
# Chạy với Unity version mặc định (6000.2.6f2)
.\Scripts\Install-AndroidBuildSupport.ps1

# Chạy với Unity version khác
.\Scripts\Install-AndroidBuildSupport.ps1 -UnityVersion "2022.3.10f1"
```

**Khi nào cần chạy:**
- Khi gặp lỗi "build target was unsupported" trên Jenkins
- Khi cài Unity mới và cần setup Android build
- Khi thiếu Android SDK/NDK/JDK

**Output:**
- ✅ Cài đặt Android Build Support
- ✅ Cài đặt Android SDK & NDK Tools  
- ✅ Cài đặt OpenJDK
- ✅ Verify cài đặt thành công

---

### 2. `Test-AndroidBuild.ps1`
**Mục đích:** Test Android build locally trước khi chạy trên Jenkins

**Sử dụng:**
```powershell
# Build với config mặc định
.\Scripts\Test-AndroidBuild.ps1

# Build với version tùy chỉnh
.\Scripts\Test-AndroidBuild.ps1 -VersionNumber "1.2.0" -BuildNumber "42"

# Build với Unity version khác
.\Scripts\Test-AndroidBuild.ps1 -UnityVersion "2022.3.10f1"
```

**Parameters:**
- `-UnityVersion`: Unity version (default: "6000.2.6f2")
- `-ProjectPath`: Đường dẫn project (default: thư mục cha của Scripts)
- `-BuildPath`: Đường dẫn output (default: "Builds/Android")
- `-VersionNumber`: Version number (default: "1.0.0")
- `-BuildNumber`: Build number (default: "999")

**Output:**
- APK file tại: `Builds/Android/[version]/[ProductName].apk`
- Build log tại: `unity-test-build.log`

---

## 🚀 Workflow: Khắc Phục Lỗi Android Build

### Bước 1: Cài Đặt Android Build Support
```powershell
cd "E:\Sample Projects\Git Practicing\Practice-Project"
.\Scripts\Install-AndroidBuildSupport.ps1
```

**Kết quả mong đợi:**
```
✅ Android Build Support
✅ Android SDK & NDK Tools
✅ OpenJDK
🎉 BẠN ĐÃ SẴN SÀNG BUILD ANDROID!
```

### Bước 2: Test Build Locally
```powershell
.\Scripts\Test-AndroidBuild.ps1 -VersionNumber "1.0.0" -BuildNumber "1"
```

**Kết quả mong đợi:**
```
✅ BUILD THÀNH CÔNG!
APK File:
  Path: E:\...\Builds\Android\1.0.0\YourGame.apk
  Size: 45.32 MB
```

### Bước 3: Test APK Trên Thiết Bị
```powershell
# Cài APK lên thiết bị Android đã kết nối
adb devices
adb install -r "Builds\Android\1.0.0\YourGame.apk"
```

### Bước 4: Chạy Jenkins Build
Nếu build local thành công:
1. Commit changes vào Git
2. Push lên repository
3. Chạy Jenkins job
4. Build sẽ thành công trên Jenkins! 🎉

---

## 🐛 Troubleshooting

### Lỗi: "execution of scripts is disabled on this system"

**Nguyên nhân:** PowerShell execution policy bị hạn chế

**Giải pháp:**
```powershell
# Cho phép chạy scripts (Chỉ cần chạy 1 lần)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Hoặc chạy script với bypass:
powershell -ExecutionPolicy Bypass -File ".\Scripts\Install-AndroidBuildSupport.ps1"
```

---

### Lỗi: "Unity Hub not found"

**Nguyên nhân:** Unity Hub không cài đặt hoặc cài ở vị trí khác

**Giải pháp:**
```powershell
# Chỉ định đường dẫn Unity Hub
.\Scripts\Install-AndroidBuildSupport.ps1 -UnityHubPath "D:\Unity Hub\Unity Hub.exe"
```

---

### Lỗi: "Unity version not found"

**Nguyên nhân:** Unity version chưa cài đặt

**Giải pháp:**
1. Mở Unity Hub
2. Installs → Add
3. Chọn Unity 6000.2.6f2 (hoặc version bạn cần)
4. Install (chưa cần chọn Android modules)
5. Chạy lại script `Install-AndroidBuildSupport.ps1`

---

### Lỗi: Build thành công nhưng không có APK

**Nguyên nhân:** Có thể là scenes chưa được add vào Build Settings

**Giải pháp:**
1. Mở Unity Editor
2. File → Build Settings
3. Add Open Scenes (hoặc kéo thả scenes vào list)
4. Chạy lại build

---

## 📊 Jenkins Integration

Các scripts này được thiết kế để chạy trên Jenkins Windows agent.

### Setup Jenkins:

#### 1. Cài đặt Android Build Support (Chạy 1 lần)
```groovy
// Jenkinsfile - Setup stage
stage('Setup Android') {
    steps {
        powershell '''
            .\\Scripts\\Install-AndroidBuildSupport.ps1 -UnityVersion "6000.2.6f2"
        '''
    }
}
```

#### 2. Build Android APK
```groovy
// Jenkinsfile - Build stage
stage('Build Android') {
    steps {
        bat """
            "${UNITY_PATH}" -quit -batchmode ^
            -buildTarget Android ^
            -projectPath "${PROJECT_PATH}" ^
            -executeMethod BuildScript.BuildAndroidAPK ^
            -buildPath "${ANDROID_BUILD_PATH}" ^
            -versionNumber ${VERSION} ^
            -buildNumber ${BUILD_NUM} ^
            -logFile "${WORKSPACE}\\unity-build-apk.log"
        """
    }
}
```

**Quan trọng:** Đảm bảo thêm `-buildTarget Android` vào Unity command line!

---

## 📝 Checklist: Android Build Setup

- [ ] Unity Hub đã cài đặt
- [ ] Unity 6000.2.6f2 (hoặc version tương ứng) đã cài đặt
- [ ] **Chạy `Install-AndroidBuildSupport.ps1`** ✅
- [ ] Verify Android modules trong Unity Hub
- [ ] **Chạy `Test-AndroidBuild.ps1`** để test local ✅
- [ ] APK build thành công local
- [ ] Test APK trên thiết bị Android
- [ ] Setup Jenkins credentials (keystore, passwords)
- [ ] Update Jenkinsfile với `-buildTarget Android`
- [ ] Push code lên Git
- [ ] Chạy Jenkins build
- [ ] Jenkins build thành công! 🎉

---

## 🔗 Liên Kết Hữu Ích

- **Unity Manual - Command Line Arguments:**  
  https://docs.unity3d.com/Manual/CommandLineArguments.html

- **Unity Manual - Android Build Settings:**  
  https://docs.unity3d.com/Manual/android-BuildSettings.html

- **Unity Hub CLI Documentation:**  
  https://docs.unity3d.com/hub/manual/HubCLI.html

- **Jenkins Pipeline Syntax:**  
  https://www.jenkins.io/doc/book/pipeline/syntax/

---

## 📞 Support

Nếu gặp vấn đề, cung cấp thông tin sau:

1. **Unity Version:** `6000.2.6f2` (hoặc version bạn dùng)
2. **Script output:** Copy toàn bộ output từ PowerShell
3. **Unity log:** File `unity-test-build.log`
4. **Installed modules:** Screenshot từ Unity Hub → Installs

---

## 📄 License

Scripts này là phần của Practice-Project và có thể tự do sử dụng/chỉnh sửa.

---

**Cập nhật lần cuối:** 16/11/2025  
**Tác giả:** Build Automation Team

