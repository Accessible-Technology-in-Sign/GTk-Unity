using System;
using SLRGTk.Camera;
using SLRGTk.Common;
using UnityEngine;

namespace SLRGTk.Engine {
    public class CameraTestEngine: MonoBehaviour {
        public StreamCamera camera;

        void Awake() {
            PermissionManager.RequestCameraPermission();
            camera.Poll();
        }
    }
}