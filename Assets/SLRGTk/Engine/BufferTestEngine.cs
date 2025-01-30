// using System;
// using System.IO;
// using Mediapipe.Tasks.Vision.HandLandmarker;
// using SLRGTk.Camera;
// using SLRGTk.Common;
// using SLRGTk.Model;
// using UnityEngine;
//
// namespace SLRGTk.Engine {
//     public class BufferTestEngine : MonoBehaviour {
//         public StreamCamera camera;
//         public MPHands mp;
//         public Buffer<HandLandmarkerResult> buffer = new();
//         public void Awake() {
//             camera = gameObject.AddComponent<StreamCamera>();
//             mp = new MPHands(Resources.Load<TextAsset>("hand_landmarker.task").bytes);
//             PermissionManager.RequestCameraPermission();
//             camera.AddCallback("ImageReceiver", image => {
//                 mp.Run(new MPVisionInput(image, DateTimeOffset.Now.ToUnixTimeMilliseconds()));
//             });
//             mp.AddCallback("BufferFiller", output => {
//                 if (output.Result.handLandmarks != null && output.Result.handLandmarks.Count > 0) {
//                     // TODO: clear the buffer if there are too many blanks in succession
//                     buffer.AddElement(output.Result);
//                 }
//             });
//             buffer.AddCallback("BufferPrinter", result => {
//                 Debug.Log("Buffer Triggered at: " + result.Count  + " Elements");
//             });
//             camera.Poll();
//         }
//     }
// }