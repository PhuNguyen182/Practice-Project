pipeline {
    agent any
    
    // ============================================
    // PARAMETERS - Cấu hình build từ Jenkins UI
    // ============================================
    parameters {
        // Platform build
        choice(
            name: 'BUILD_TARGET',
            choices: ['All', 'Windows', 'Android', 'iOS', 'Windows+Android', 'Windows+iOS', 'Android+iOS'],
            description: 'Chọn platform cần build'
        )
        
        // Build execution mode
        choice(
            name: 'BUILD_MODE',
            choices: ['Parallel', 'Sequential'],
            description: 'Chế độ build: song song (nhanh hơn) hoặc tuần tự (ổn định hơn)'
        )
        
        // Windows options
        booleanParam(
            name: 'BUILD_WINDOWS',
            defaultValue: true,
            description: '✓ Build Windows standalone'
        )
        booleanParam(
            name: 'ZIP_WINDOWS_BUILD',
            defaultValue: true,
            description: '✓ Nén build Windows thành file ZIP'
        )
        
        // Android options
        booleanParam(
            name: 'BUILD_APK',
            defaultValue: true,
            description: '✓ Build Android APK'
        )
        booleanParam(
            name: 'BUILD_AAB',
            defaultValue: true,
            description: '✓ Build Android App Bundle (AAB)'
        )
        booleanParam(
            name: 'DEPLOY_GOOGLE_PLAY',
            defaultValue: false,
            description: '✓ Deploy AAB lên Google Play Console (internal track)'
        )
        
        // iOS options
        booleanParam(
            name: 'BUILD_IOS',
            defaultValue: true,
            description: '✓ Build iOS Xcode project'
        )
        booleanParam(
            name: 'DEPLOY_TESTFLIGHT',
            defaultValue: false,
            description: '✓ Deploy lên TestFlight'
        )
        
        // Testing
        booleanParam(
            name: 'RUN_TESTS',
            defaultValue: true,
            description: '✓ Chạy Unit Tests trước khi build'
        )
        
        // Versioning
        string(
            name: 'VERSION_NUMBER',
            defaultValue: '1.0.0',
            description: 'Version number (VD: 1.0.0)'
        )
        string(
            name: 'BUILD_NUMBER_OVERRIDE',
            defaultValue: '',
            description: 'Build number (để trống để dùng Jenkins BUILD_NUMBER)'
        )
    }
    
    // ============================================
    // ENVIRONMENT - Biến môi trường
    // ============================================
    environment {
        // Unity Configuration (TÙY CHỈNH THEO HỆ THỐNG CỦA BẠN)
        UNITY_VERSION = '6000.0.26f1'
        UNITY_PATH = "C:\\Program Files\\Unity\\Hub\\Editor\\${UNITY_VERSION}\\Editor\\Unity.exe"
        PROJECT_PATH = "${WORKSPACE}"
        
        // Build Configuration
        BUILD_PATH = "${WORKSPACE}\\Builds"
        WINDOWS_BUILD_PATH = "${BUILD_PATH}\\Windows"
        ANDROID_BUILD_PATH = "${BUILD_PATH}\\Android"
        IOS_BUILD_PATH = "${BUILD_PATH}\\iOS"
        
        // Android Keystore (Cấu hình trong Jenkins Credentials)
        ANDROID_KEYSTORE_PATH = credentials('android-keystore-file')
        ANDROID_KEYSTORE_PASS = credentials('android-keystore-password')
        ANDROID_KEY_ALIAS = credentials('android-key-alias')
        ANDROID_KEY_PASS = credentials('android-key-password')
        
        // Google Play Console (để upload AAB)
        GOOGLE_PLAY_SERVICE_ACCOUNT = credentials('google-play-service-account-json')
        GOOGLE_PLAY_PACKAGE_NAME = 'com.yourcompany.brainikkk'
        
        // iOS Configuration
        XCODE_PROJECT_PATH = "${IOS_BUILD_PATH}\\Unity-iPhone.xcodeproj"
        APPLE_ID = credentials('apple-id')
        APPLE_APP_SPECIFIC_PASSWORD = credentials('apple-app-specific-password')
        APPLE_TEAM_ID = credentials('apple-team-id')
        PROVISIONING_PROFILE = credentials('ios-provisioning-profile')
        CODE_SIGN_IDENTITY = 'Apple Distribution'
        
        // Unity License (Personal - không cần license file)
        // Nếu dùng Pro/Plus, thêm credentials tương ứng
        UNITY_LICENSE_TYPE = 'Personal'
        
        // Build Metadata
        VERSION = "${params.VERSION_NUMBER}"
        BUILD_NUM = "${params.BUILD_NUMBER_OVERRIDE ?: env.BUILD_NUMBER}"
        BUILD_NAME = "brainikkk_v${params.VERSION_NUMBER}_b${params.BUILD_NUMBER_OVERRIDE ?: env.BUILD_NUMBER}"
    }
    
    // ============================================
    // TRIGGERS - Tự động build khi có thay đổi
    // ============================================
    triggers {
        // Tự động build khi có commit mới trên main hoặc develop
        pollSCM('H/5 * * * *') // Check mỗi 5 phút
    }
    
    // ============================================
    // OPTIONS - Cấu hình pipeline
    // ============================================
    options {
        // Giữ lại 10 build gần nhất
        buildDiscarder(logRotator(numToKeepStr: '10'))
        
        // Timeout 2 giờ cho toàn bộ pipeline
        timeout(time: 2, unit: 'HOURS')
        
        // Không build cùng lúc
        disableConcurrentBuilds()
        
        // Timestamps trong log
        timestamps()
    }
    
    // ============================================
    // STAGES - Các bước thực hiện
    // ============================================
    stages {
        
        // ========================================
        // STAGE 1: Khởi tạo và kiểm tra
        // ========================================
        stage('Initialize') {
            steps {
                script {
                    echo '═══════════════════════════════════════'
                    echo '🚀 UNITY BUILD PIPELINE STARTED'
                    echo '═══════════════════════════════════════'
                    echo "📌 Branch: ${env.GIT_BRANCH}"
                    echo "📌 Version: ${VERSION}"
                    echo "📌 Build Number: ${BUILD_NUM}"
                    echo "📌 Build Target: ${params.BUILD_TARGET}"
                    echo "📌 Build Mode: ${params.BUILD_MODE}"
                    echo "═══════════════════════════════════════"
                    
                    // Kiểm tra branch để quyết định có deploy không
                    def branch = env.GIT_BRANCH ?: 'unknown'
                    if (branch.contains('main') || branch.contains('master')) {
                        env.IS_PRODUCTION = 'true'
                        env.DEPLOY_ENABLED = 'true'
                        echo "✅ Production build - Deploy enabled"
                    } else if (branch.contains('develop')) {
                        env.IS_PRODUCTION = 'false'
                        env.DEPLOY_ENABLED = 'true'
                        echo "✅ Development build - Deploy enabled"
                    } else {
                        env.IS_PRODUCTION = 'false'
                        env.DEPLOY_ENABLED = 'false'
                        echo "⚠️  Feature branch - Deploy disabled"
                    }
                    
                    // Chỉ trigger tự động trên main và develop
                    if (!branch.contains('main') && !branch.contains('master') && !branch.contains('develop')) {
                        if (currentBuild.getBuildCauses('hudson.triggers.SCMTrigger$SCMTriggerCause')) {
                            echo "⛔ Auto-build chỉ cho phép trên main/develop. Hủy build..."
                            currentBuild.result = 'ABORTED'
                            error('Auto-build chỉ được phép trên main/develop branch')
                        }
                    }
                }
            }
        }
        
        // ========================================
        // STAGE 2: Checkout code
        // ========================================
        stage('Checkout') {
            steps {
                echo '📦 Đang checkout repository...'
                checkout scm
                
                script {
                    // Clean previous builds
                    bat """
                        if exist "${BUILD_PATH}" rmdir /S /Q "${BUILD_PATH}"
                        mkdir "${WINDOWS_BUILD_PATH}"
                        mkdir "${ANDROID_BUILD_PATH}"
                        mkdir "${IOS_BUILD_PATH}"
                    """
                }
            }
        }
        
        // ========================================
        // STAGE 3: Unity Tests (Optional)
        // ========================================
        stage('Run Unity Tests') {
            when {
                expression { params.RUN_TESTS == true }
            }
            steps {
                echo '🧪 Đang chạy Unity Tests...'
                script {
                    bat """
                        "${UNITY_PATH}" -batchmode -nographics ^
                        -projectPath "${PROJECT_PATH}" ^
                        -runTests ^
                        -testPlatform EditMode ^
                        -testResults "${WORKSPACE}\\test-results-editmode.xml" ^
                        -logFile "${WORKSPACE}\\unity-test-editmode.log"
                    """
                    
                    bat """
                        "${UNITY_PATH}" -batchmode -nographics ^
                        -projectPath "${PROJECT_PATH}" ^
                        -runTests ^
                        -testPlatform PlayMode ^
                        -testResults "${WORKSPACE}\\test-results-playmode.xml" ^
                        -logFile "${WORKSPACE}\\unity-test-playmode.log"
                    """
                }
            }
        }
        
        // ========================================
        // STAGE 4: Build Platforms
        // ========================================
        stage('Build Platforms') {
            steps {
                script {
                    // Quyết định build mode: Parallel hoặc Sequential
                    if (params.BUILD_MODE == 'Parallel') {
                        echo '⚡ Build mode: PARALLEL (nhanh hơn)'
                        buildPlatformsParallel()
                    } else {
                        echo '📝 Build mode: SEQUENTIAL (ổn định hơn)'
                        buildPlatformsSequential()
                    }
                }
            }
        }
        
        // ========================================
        // STAGE 5: Post-Build Processing
        // ========================================
        stage('Post-Build Processing') {
            steps {
                script {
                    echo '📦 Đang xử lý builds...'
                    
                    // Nén Windows build nếu cần
                    if (shouldBuildWindows() && params.ZIP_WINDOWS_BUILD) {
                        echo '🗜️ Đang nén Windows build...'
                        bat """
                            powershell -Command "Compress-Archive -Path '${WINDOWS_BUILD_PATH}\\${VERSION}\\*' -DestinationPath '${WINDOWS_BUILD_PATH}\\${BUILD_NAME}_Windows.zip' -Force"
                        """
                    }
                }
            }
        }
        
        // ========================================
        // STAGE 6: Deploy (if enabled)
        // ========================================
        stage('Deploy') {
            when {
                expression { env.DEPLOY_ENABLED == 'true' }
            }
            steps {
                script {
                    parallel(
                        'Deploy to Google Play': {
                            if (params.BUILD_AAB && params.DEPLOY_GOOGLE_PLAY) {
                                deployToGooglePlay()
                            }
                        },
                        'Deploy to TestFlight': {
                            if (params.BUILD_IOS && params.DEPLOY_TESTFLIGHT) {
                                deployToTestFlight()
                            }
                        }
                    )
                }
            }
        }
        
        // ========================================
        // STAGE 7: Archive Artifacts
        // ========================================
        stage('Archive Artifacts') {
            steps {
                echo '📦 Đang lưu trữ artifacts...'
                script {
                    // Archive builds
                    archiveArtifacts artifacts: 'Builds/**/*.zip', allowEmptyArchive: true, fingerprint: true
                    archiveArtifacts artifacts: 'Builds/**/*.exe', allowEmptyArchive: true, fingerprint: true
                    archiveArtifacts artifacts: 'Builds/**/*.apk', allowEmptyArchive: true, fingerprint: true
                    archiveArtifacts artifacts: 'Builds/**/*.aab', allowEmptyArchive: true, fingerprint: true
                    archiveArtifacts artifacts: 'Builds/**/*.ipa', allowEmptyArchive: true, fingerprint: true
                    
                    // Archive logs
                    archiveArtifacts artifacts: '*.log', allowEmptyArchive: true
                }
            }
        }
    }
    
    // ============================================
    // POST ACTIONS - Sau khi build xong
    // ============================================
    post {
        always {
            echo '🧹 Cleaning up...'
            script {
                // Publish test results nếu có
                junit testResults: 'test-results*.xml', allowEmptyResults: true
            }
        }
        
        success {
            echo '✅ ═══════════════════════════════════════'
            echo '✅ BUILD SUCCEEDED!'
            echo '✅ ═══════════════════════════════════════'
            script {
                // Gửi thông báo (có thể tích hợp Slack, Email, etc.)
                def message = "✅ Build #${BUILD_NUM} thành công cho ${env.GIT_BRANCH}\nVersion: ${VERSION}"
                echo message
                // slackSend(color: 'good', message: message)
            }
        }
        
        failure {
            echo '❌ ═══════════════════════════════════════'
            echo '❌ BUILD FAILED!'
            echo '❌ ═══════════════════════════════════════'
            script {
                // Gửi thông báo lỗi
                def message = "❌ Build #${BUILD_NUM} thất bại cho ${env.GIT_BRANCH}"
                echo message
                // slackSend(color: 'danger', message: message)
                
                // Archive failure logs
                archiveArtifacts artifacts: '*.log', allowEmptyArchive: true
            }
        }
        
        cleanup {
            echo '🗑️ Final cleanup...'
            // Có thể clean workspace nếu cần
            // cleanWs()
        }
    }
}

// ============================================
// HELPER FUNCTIONS
// ============================================

// Build platforms in PARALLEL mode
def buildPlatformsParallel() {
    def builds = [:]
    
    if (shouldBuildWindows()) {
        builds['Windows'] = {
            stage('Build Windows') {
                buildWindows()
            }
        }
    }
    
    if (shouldBuildAndroid()) {
        builds['Android'] = {
            stage('Build Android') {
                if (params.BUILD_APK) {
                    buildAndroidAPK()
                }
                if (params.BUILD_AAB) {
                    buildAndroidAAB()
                }
            }
        }
    }
    
    if (shouldBuildIOS()) {
        builds['iOS'] = {
            stage('Build iOS') {
                buildIOS()
            }
        }
    }
    
    if (builds.isEmpty()) {
        echo '⚠️  Không có platform nào được chọn để build!'
    } else {
        parallel builds
    }
}

// Build platforms in SEQUENTIAL mode
def buildPlatformsSequential() {
    if (shouldBuildWindows()) {
        stage('Build Windows') {
            buildWindows()
        }
    }
    
    if (shouldBuildAndroid()) {
        if (params.BUILD_APK) {
            stage('Build Android APK') {
                buildAndroidAPK()
            }
        }
        if (params.BUILD_AAB) {
            stage('Build Android AAB') {
                buildAndroidAAB()
            }
        }
    }
    
    if (shouldBuildIOS()) {
        stage('Build iOS') {
            buildIOS()
        }
    }
}

// Check if should build Windows
def shouldBuildWindows() {
    return params.BUILD_WINDOWS && 
           (params.BUILD_TARGET == 'All' || 
            params.BUILD_TARGET == 'Windows' || 
            params.BUILD_TARGET.contains('Windows'))
}

// Check if should build Android
def shouldBuildAndroid() {
    return (params.BUILD_APK || params.BUILD_AAB) && 
           (params.BUILD_TARGET == 'All' || 
            params.BUILD_TARGET == 'Android' || 
            params.BUILD_TARGET.contains('Android'))
}

// Check if should build iOS
def shouldBuildIOS() {
    return params.BUILD_IOS && 
           (params.BUILD_TARGET == 'All' || 
            params.BUILD_TARGET == 'iOS' || 
            params.BUILD_TARGET.contains('iOS'))
}

// Build Windows
def buildWindows() {
    echo '🪟 Building Windows...'
    bat """
        "${UNITY_PATH}" -quit -batchmode -nographics ^
        -projectPath "${PROJECT_PATH}" ^
        -executeMethod BuildScript.BuildWindows ^
        -buildPath "${WINDOWS_BUILD_PATH}" ^
        -versionNumber ${VERSION} ^
        -buildNumber ${BUILD_NUM} ^
        -logFile "${WORKSPACE}\\unity-build-windows.log"
    """
}

// Build Android APK
def buildAndroidAPK() {
    echo '🤖 Building Android APK...'
    bat """
        "${UNITY_PATH}" -quit -batchmode -nographics ^
        -projectPath "${PROJECT_PATH}" ^
        -executeMethod BuildScript.BuildAndroidAPK ^
        -buildPath "${ANDROID_BUILD_PATH}" ^
        -keystorePath "${ANDROID_KEYSTORE_PATH}" ^
        -keystorePass "${ANDROID_KEYSTORE_PASS}" ^
        -keyaliasName "${ANDROID_KEY_ALIAS}" ^
        -keyaliasPass "${ANDROID_KEY_PASS}" ^
        -versionNumber ${VERSION} ^
        -buildNumber ${BUILD_NUM} ^
        -logFile "${WORKSPACE}\\unity-build-apk.log"
    """
}

// Build Android AAB
def buildAndroidAAB() {
    echo '📦 Building Android AAB...'
    bat """
        "${UNITY_PATH}" -quit -batchmode -nographics ^
        -projectPath "${PROJECT_PATH}" ^
        -executeMethod BuildScript.BuildAndroidAAB ^
        -buildPath "${ANDROID_BUILD_PATH}" ^
        -keystorePath "${ANDROID_KEYSTORE_PATH}" ^
        -keystorePass "${ANDROID_KEYSTORE_PASS}" ^
        -keyaliasName "${ANDROID_KEY_ALIAS}" ^
        -keyaliasPass "${ANDROID_KEY_PASS}" ^
        -versionNumber ${VERSION} ^
        -buildNumber ${BUILD_NUM} ^
        -logFile "${WORKSPACE}\\unity-build-aab.log"
    """
}

// Build iOS
def buildIOS() {
    echo '🍎 Building iOS...'
    
    // Step 1: Build Xcode project từ Unity
    bat """
        "${UNITY_PATH}" -quit -batchmode -nographics ^
        -projectPath "${PROJECT_PATH}" ^
        -executeMethod BuildScript.BuildiOS ^
        -buildPath "${IOS_BUILD_PATH}" ^
        -versionNumber ${VERSION} ^
        -buildNumber ${BUILD_NUM} ^
        -logFile "${WORKSPACE}\\unity-build-ios.log"
    """
    
    // Step 2 & 3: Build archive và export IPA (cần Xcode trên macOS)
    // Nếu bạn build trên Windows, bạn cần macOS agent riêng cho iOS
    echo '⚠️  Lưu ý: Để build IPA và deploy TestFlight, cần Xcode trên macOS'
    echo '💡 Xcode project đã được tạo tại: ${IOS_BUILD_PATH}'
    
    // Uncomment phần này nếu chạy trên macOS với Xcode
    /*
    sh """
        cd "${IOS_BUILD_PATH}"
        
        # Import provisioning profile
        mkdir -p ~/Library/MobileDevice/Provisioning\\ Profiles
        cp "${PROVISIONING_PROFILE}" ~/Library/MobileDevice/Provisioning\\ Profiles/
        
        # Build archive
        xcodebuild -project Unity-iPhone.xcodeproj \\
            -scheme Unity-iPhone \\
            -configuration Release \\
            -archivePath "${IOS_BUILD_PATH}/${BUILD_NAME}.xcarchive" \\
            archive \\
            CODE_SIGN_IDENTITY="${CODE_SIGN_IDENTITY}" \\
            DEVELOPMENT_TEAM="${APPLE_TEAM_ID}" \\
            -allowProvisioningUpdates
        
        # Create ExportOptions.plist
        cat > ExportOptions.plist << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>method</key>
    <string>app-store</string>
    <key>teamID</key>
    <string>${APPLE_TEAM_ID}</string>
    <key>uploadSymbols</key>
    <true/>
</dict>
</plist>
EOF
        
        # Export IPA
        xcodebuild -exportArchive \\
            -archivePath "${IOS_BUILD_PATH}/${BUILD_NAME}.xcarchive" \\
            -exportPath "${IOS_BUILD_PATH}" \\
            -exportOptionsPlist ExportOptions.plist \\
            -allowProvisioningUpdates
        
        # Rename IPA
        mv *.ipa "${BUILD_NAME}.ipa"
    """
    */
}

// Deploy to Google Play
def deployToGooglePlay() {
    echo '🚀 Deploying to Google Play Console...'
    
    // Sử dụng fastlane hoặc Google Play Publisher API
    bat """
        pip install google-play-scraper google-auth google-auth-oauthlib google-auth-httplib2 google-api-python-client
        
        python "${WORKSPACE}\\Scripts\\deploy_google_play.py" ^
            --aab "${ANDROID_BUILD_PATH}\\${VERSION}" ^
            --service-account "${GOOGLE_PLAY_SERVICE_ACCOUNT}" ^
            --package-name "${GOOGLE_PLAY_PACKAGE_NAME}" ^
            --track "internal"
    """
    
    echo '✅ Deployed to Google Play (internal track)'
}

// Deploy to TestFlight
def deployToTestFlight() {
    echo '🚀 Deploying to TestFlight...'
    
    // Cần chạy trên macOS với Xcode
    // Uncomment nếu chạy trên macOS
    /*
    sh """
        # Validate app
        xcrun altool --validate-app \\
            -f "${IOS_BUILD_PATH}/${BUILD_NAME}.ipa" \\
            -t ios \\
            -u "${APPLE_ID}" \\
            -p "${APPLE_APP_SPECIFIC_PASSWORD}"
        
        # Upload to TestFlight
        xcrun altool --upload-app \\
            -f "${IOS_BUILD_PATH}/${BUILD_NAME}.ipa" \\
            -t ios \\
            -u "${APPLE_ID}" \\
            -p "${APPLE_APP_SPECIFIC_PASSWORD}"
    """
    
    echo '✅ Deployed to TestFlight'
    */
    
    echo '⚠️  Deploy TestFlight cần macOS với Xcode'
}

