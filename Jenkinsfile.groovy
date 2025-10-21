def PROJECT_NAME = "Practice-Project"
def CUSTOM_WORKSPACE = "E:\\Sample Projects\\Git Practicing\\${PROJECT_NAME}"
def UNITY_INSTALLATION = "C:\\Program Files\\Unity\\Hub\\Editor\\6000.2.6f2\\Editor"

pipeline {
    environment {
        PROJECT_PATH = "${CUSTOM_WORKSPACE}"
    }

    agent {
        label {
            label ""
            customWorkspace "${CUSTOM_WORKSPACE}"
        }
    }

    stages {
        stage('Build Windows') {
            when{
                expression {
                    BUILD_WINDOWS == 'true'
                }
            }
            steps {
                script {
                    withEnv(["UNITY_PATH=${UNITY_INSTALLATION}"]) {

                    }
                }
            }
        }

        stage('Deploy Windows') {
            when{
                expression {
                    DEPLOY_WINDOWS == 'true'
                }
            }

        }
    }
}