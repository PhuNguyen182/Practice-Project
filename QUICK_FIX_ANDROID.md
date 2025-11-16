# ⚡ Quick Fix: Android Build Error

> **Lỗi:** `Build Error: Error building player because build target was unsupported`

---

## 🚨 TL;DR - Fix Nhanh (5 phút)

```powershell
# 1. Mở PowerShell tại thư mục project
cd "E:\Sample Projects\Git Practicing\Practice-Project"

# 2. Cài Android Build Support
.\Scripts\Install-AndroidBuildSupport.ps1

# 3. Test build
.\Scripts\Test-AndroidBuild.ps1

# 4. Chạy lại Jenkins build → Thành công! ✅
```

---

## 📋 Checklist Nhanh

```
[ ] Đã cài Unity Hub
[ ] Đã cài Unity 6000.2.6f2
[ ] Chạy Install-AndroidBuildSupport.ps1
[ ] Thấy: ✅ Android Build Support
[ ] Thấy: ✅ Android SDK & NDK Tools  
[ ] Thấy: ✅ OpenJDK
[ ] Test-AndroidBuild.ps1 thành công
[ ] APK file được tạo
[ ] Chạy Jenkins build → SUCCESS! 🎉
```

---

## 🎯 Nguyên Nhân

❌ **Unity Editor trên Jenkins CHƯA CÀI Android Build Support**

---

## ✅ Giải Pháp

### Option 1: Tự Động (Khuyến Nghị)
```powershell
.\Scripts\Install-AndroidBuildSupport.ps1
```

### Option 2: Thủ Công
1. Mở **Unity Hub**
2. **Installs** → Unity **6000.2.6f2** → **⚙️** → **Add Modules**
3. Chọn:
   - ✓ Android Build Support
   - ✓ Android SDK & NDK Tools
   - ✓ OpenJDK
4. **Done** → Chờ cài (5-15 phút)

---

## 🧪 Test Ngay

```powershell
# Test local trước khi chạy Jenkins
.\Scripts\Test-AndroidBuild.ps1

# Nếu thấy "✅ BUILD THÀNH CÔNG!" → Sẵn sàng cho Jenkins
```

---

## 📞 Gặp Vấn Đề?

### ❌ "Unity Hub not found"
```powershell
# Chỉ định đường dẫn Unity Hub
.\Scripts\Install-AndroidBuildSupport.ps1 -UnityHubPath "D:\Path\To\Unity Hub.exe"
```

### ❌ "execution of scripts is disabled"
```powershell
# Cho phép chạy scripts
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### ❌ Build thành công nhưng không có APK
1. Mở Unity Editor
2. File → Build Settings
3. Add Open Scenes
4. Chạy lại build

---

## 📚 Đọc Thêm

- **Chi tiết:** `ANDROID_BUILD_FIX.md`
- **Scripts:** `Scripts/README.md`
- **Changelog:** `CHANGELOG.md`

---

**Tóm lại: Chạy script → Cài Android support → Done! ✅**

