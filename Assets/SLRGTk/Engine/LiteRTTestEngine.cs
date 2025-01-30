using System;
using System.Buffers;
using System.IO;
using System.Linq;
using Mediapipe;
using Mediapipe.Tasks.Vision.HandLandmarker;
using SLRGTk.Camera;
using SLRGTk.Common;
using SLRGTk.Model;
using UnityEngine;

namespace SLRGTk.Engine {
    public class LiteRTTestEngine : MonoBehaviour {
        public StreamCamera camera;
        public MPHands mp;
        public Buffer<HandLandmarkerResult> buffer = new();
        public LiteRTPopsignIsolatedSLR recognizer;
        public void Awake() {
            mp = new MPHands(Resources.Load<TextAsset>("hand_landmarker.task").bytes);
            recognizer = new LiteRTPopsignIsolatedSLR(Resources.Load<TextAsset>("563-double-lstm-120-cpu.tflite").bytes,
                Resources.Load<TextAsset>("signsList").text.Split("\n").Select(line => line.Trim()).ToList());
            camera = gameObject.AddComponent<StreamCamera>();
            PermissionManager.RequestCameraPermission();
            camera.AddCallback("ImageReceiver", image => {
                Debug.Log("Event");
                mp.Run(new MPVisionInput(image, DateTimeOffset.Now.ToUnixTimeMilliseconds(), 720, 1280));
            });
            mp.AddCallback("BufferFiller", output => {
                Debug.Log(output.ToString() + ", " + (output.Result.handLandmarks != null && output.Result.handLandmarks.Count > 0));
                if (output.Result.handLandmarks != null && output.Result.handLandmarks.Count > 0) {
                    // TODO: clear the buffer if there are too many blanks in succession
                    buffer.AddElement(output.Result);
                }
                // CustomTextureManager.ScheduleDeletion(output.OriginalImage);
            });
            buffer.AddCallback("BufferPrinter", result => {
                Debug.Log("Buffer Triggered at: " + result.Count  + " Elements");
            });
            buffer.AddCallback("RecoginzerChain", result => {
                recognizer.Run(new(result));
                buffer.Clear();
            });
            recognizer.AddCallback("Recoginzer Result", result => {
                Debug.Log("Recoginzer Result: " + result);
            });
            camera.Poll();
        }
    }
}
