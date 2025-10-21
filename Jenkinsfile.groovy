def PROJECT_NAME = "Practice-Project"
def CUSTOM_WORKSPACE = "E:\\Sample Projects\\Git Practicing"
def UNITY_VERSION = "6000.2.6f2"
def UNITY_INSTALLATION = "C:\\Program Files\\Unity\\Hub\\Editor\\${UNITY_VERSION}\\Editor"

pipeline {
    environment {
        PROJECT_PATH = "${CUSTOM_WORKSPACE}\\${PROJECT_NAME}"
    }

    agent {
        label {
            label "Windows Build"
            customWorkspace "${CUSTOM_WORKSPACE}\\${PROJECT_NAME}"
        }
    }

    stages {
        stage("Build Windows") {
            when { expression { BUILD_WINDOWS == 'true'}}
            steps {
                script {
                    withEnv(["UNITY_PATH=${UNITY_INSTALLATION}"]) {
                        bat '''
                        "%UNITY_PATH%/Unity.exe" -quit -batchmode -projectPath %PROJECT_PATH% -executeMethod BuildScript.BuildWindows -logFile -
                        '''
                    }
                }
            }
        }

        stage("Deploy Windows") {
            when{ expression { DEPLOY_WINDOWS == 'true' }}
            steps {
                echo 'Deploying Windows...'
            }
        }
    }
}