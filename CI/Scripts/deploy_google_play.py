"""
Script deploy Android AAB lên Google Play Console
Sử dụng Google Play Developer API
Hỗ trợ tìm file AAB trong folder version
"""

import argparse
import sys
import os
import glob
from google.oauth2 import service_account
from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload

def find_aab_file(path):
    """
    Tìm file AAB trong path
    Path có thể là:
    - File AAB trực tiếp: path/to/game.aab
    - Folder chứa AAB: path/to/folder (sẽ tìm *.aab trong folder)
    - Folder version: Builds/Android/1.0.0 (sẽ tìm *.aab trong folder)
    """
    # Nếu là file AAB trực tiếp
    if os.path.isfile(path) and path.endswith('.aab'):
        return path
    
    # Nếu là folder, tìm file AAB đầu tiên
    if os.path.isdir(path):
        aab_files = glob.glob(os.path.join(path, '*.aab'))
        if aab_files:
            return aab_files[0]  # Trả về file AAB đầu tiên
        else:
            raise FileNotFoundError(f"Không tìm thấy file AAB trong folder: {path}")
    
    raise FileNotFoundError(f"Không tìm thấy file AAB: {path}")

def deploy_to_google_play(aab_path, service_account_json, package_name, track='internal'):
    """
    Deploy AAB file lên Google Play Console
    
    Args:
        aab_path: Đường dẫn đến file AAB hoặc folder chứa AAB
        service_account_json: Đường dẫn đến file JSON service account
        package_name: Package name của app (VD: com.company.game)
        track: Track để deploy (internal, alpha, beta, production)
    """
    
    print("========================================")
    print("🚀 Deploying to Google Play Console...")
    print("========================================")
    
    # Tìm file AAB
    try:
        actual_aab_path = find_aab_file(aab_path)
        print(f"✅ Found AAB: {actual_aab_path}")
    except FileNotFoundError as e:
        print(f"❌ {str(e)}")
        return 1
    
    print(f"Package: {package_name}")
    print(f"Track: {track}")
    print("========================================")
    
    try:
        # Authenticate với Google Play API
        print("🔑 Authenticating with Google Play API...")
        credentials = service_account.Credentials.from_service_account_file(
            service_account_json,
            scopes=['https://www.googleapis.com/auth/androidpublisher']
        )
        
        service = build('androidpublisher', 'v3', credentials=credentials)
        
        # Tạo edit session
        print("📝 Creating edit session...")
        edit_request = service.edits().insert(packageName=package_name)
        edit_response = edit_request.execute()
        edit_id = edit_response['id']
        print(f"Edit ID: {edit_id}")
        
        # Upload AAB
        print("📦 Uploading AAB...")
        media = MediaFileUpload(actual_aab_path, mimetype='application/octet-stream', resumable=True)
        upload_request = service.edits().bundles().upload(
            editId=edit_id,
            packageName=package_name,
            media_body=media
        )
        upload_response = upload_request.execute()
        version_code = upload_response['versionCode']
        print(f"✅ Uploaded! Version Code: {version_code}")
        
        # Assign to track
        print(f"🎯 Assigning to '{track}' track...")
        track_request = service.edits().tracks().update(
            editId=edit_id,
            track=track,
            packageName=package_name,
            body={
                'releases': [{
                    'versionCodes': [version_code],
                    'status': 'completed',
                }]
            }
        )
        track_response = track_request.execute()
        print(f"✅ Assigned to track: {track_response['track']}")
        
        # Commit changes
        print("💾 Committing changes...")
        commit_request = service.edits().commit(
            editId=edit_id,
            packageName=package_name
        )
        commit_response = commit_request.execute()
        print(f"✅ Committed! Edit ID: {commit_response['id']}")
        
        print("========================================")
        print("✅ DEPLOY SUCCEEDED!")
        print(f"Version {version_code} deployed to {track} track")
        print("========================================")
        
        return 0
        
    except Exception as e:
        print("========================================")
        print("❌ DEPLOY FAILED!")
        print(f"Error: {str(e)}")
        print("========================================")
        return 1

def main():
    parser = argparse.ArgumentParser(description='Deploy AAB to Google Play Console')
    parser.add_argument('--aab', required=True, help='Path to AAB file or folder containing AAB')
    parser.add_argument('--service-account', required=True, help='Path to service account JSON')
    parser.add_argument('--package-name', required=True, help='App package name')
    parser.add_argument('--track', default='internal', choices=['internal', 'alpha', 'beta', 'production'],
                       help='Release track (default: internal)')
    
    args = parser.parse_args()
    
    sys.exit(deploy_to_google_play(
        aab_path=args.aab,
        service_account_json=args.service_account,
        package_name=args.package_name,
        track=args.track
    ))

if __name__ == '__main__':
    main()

