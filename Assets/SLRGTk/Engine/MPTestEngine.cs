// using System;
// using System.IO;
// using SLRGTk.Camera;
// using SLRGTk.Common;
// using SLRGTk.Model;
// using UnityEngine;
//
// namespace SLRGTk.Engine {
//     public class MPTestEngine : MonoBehaviour {
//         public StreamCamera camera;
//         public MPHands mp;
//
//         public void Awake() {
//             mp = new MPHands(Resources.Load<TextAsset>("hand_landmarker.task").bytes);
//             camera = gameObject.AddComponent<StreamCamera>();
//             PermissionManager.RequestCameraPermission();
//             camera.AddCallback("ImageReceiver", image => {
//                 mp.Run(new MPVisionInput(image, DateTimeOffset.Now.ToUnixTimeMilliseconds()));
//             });
//             camera.Poll();
//         }
//     }
// }