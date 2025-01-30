using UnityEngine;

namespace SLRGTk.Common {
    public class PermissionManager {
        public static void RequestCameraPermission() {
#if UNITY_IOS && !UNITY_EDITOR
            Application.RequestUserAuthorization(UserAuthorization.WebCam);
#elif UNITY_ANDROID && !UNITY_EDITOR
            UnityEngine.Android.Permission.RequestUserPermission("android.permission.CAMERA");
#endif
        }
    }
}

    
