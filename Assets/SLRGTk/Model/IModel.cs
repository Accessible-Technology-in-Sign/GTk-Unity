using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Mediapipe;
using Mediapipe.Tasks.Vision.HandLandmarker;
using SLRGTk.Common;
using TensorFlowLite;
using Unity.Collections;
using UnityEngine;

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif

namespace SLRGTk.Model {
    public interface IModel <I> {
        void Run(I input);
    }
    
    public class MPVisionInput {
        public NativeArray<byte> Image { get; }
        public long Timestamp { get; }
        
        public int Width { get; }
        public int Height { get; }

        public MPVisionInput(NativeArray<byte> image, long timestamp, int width, int height) {
            Image = image;
            Timestamp = timestamp;
            Width = width;
            Height = height;
        }
    }

    public class MPHandsOutput {
        public NativeArray<byte> OriginalImage { get; }
        public HandLandmarkerResult Result { get; }

        public MPHandsOutput(NativeArray<byte> originalImage, HandLandmarkerResult result) {
            OriginalImage = originalImage;
            Result = result;
        }
    }
    public class MPHands : CallbackManager<MPHandsOutput>, IModel<MPVisionInput>
    {
        private readonly HandLandmarker graph;
        private readonly ConcurrentDictionary<long, NativeArray<byte>> outputInputLookup = new();
        private readonly Mediapipe.Tasks.Vision.Core.RunningMode runningMode;

        // Constructor with all relevant parameters including the model asset buffer
        public MPHands(
            byte[] modelAssetBuffer,
            Mediapipe.Tasks.Vision.Core.RunningMode runningMode = Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM,
            float handDetectionConfidence = 0.5f,
            float trackingConfidence = 0.5f,
            float handPresenceConfidence = 0.5f,
            int numHands = 1
        )
        {
            this.runningMode = runningMode;

            // If running in live stream mode, set the result callback
            if (runningMode == Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM) {
                graph = HandLandmarker.CreateFromOptions(new HandLandmarkerOptions(
                    new Mediapipe.Tasks.Core.BaseOptions(modelAssetBuffer: modelAssetBuffer),
                    resultCallback: (i, pipelineImage, timestampMs) => {
                        if (!outputInputLookup.TryGetValue(timestampMs, out var texture)) return;

                        // Trigger callbacks with the current result and input texture
                        foreach (var cb in callbacks.Values) {
                            cb(new MPHandsOutput(originalImage: texture, result: i));
                        }
                        

                        // Remove the current timestamp entry
                        outputInputLookup.Remove(timestampMs, out _);

                        // Collect old timestamps
                        var oldTimestamps = outputInputLookup.Keys.Where(ts => ts < timestampMs).ToList();

                        // Remove old timestamps and schedule texture deletion
                        foreach (var oldTimestamp in oldTimestamps) {
                            if (!outputInputLookup.TryGetValue(oldTimestamp, out var oldTexture)) continue;
                            // CustomTextureManager.ScheduleDeletion(oldTexture);]
                            oldTexture.Dispose();
                            outputInputLookup.Remove(oldTimestamp, out _);
                        }
                    },
                    minHandDetectionConfidence: handDetectionConfidence,
                    minTrackingConfidence: trackingConfidence,
                    minHandPresenceConfidence: handPresenceConfidence,
                    numHands: numHands,
                    runningMode: runningMode
                ));
            }
            else {
                graph = HandLandmarker.CreateFromOptions(new HandLandmarkerOptions(
                    new Mediapipe.Tasks.Core.BaseOptions(modelAssetBuffer: modelAssetBuffer),
                    minHandDetectionConfidence: handDetectionConfidence,
                    minTrackingConfidence: trackingConfidence,
                    minHandPresenceConfidence: handPresenceConfidence,
                    numHands: numHands,
                    runningMode: runningMode
                ));
            }
        }

        public void Run(MPVisionInput input)
        {
            var img = new Image(TextureFormat.RGBA32.ToImageFormat(), input.Width, input.Height, TextureFormat.RGBA32.ToImageFormat().NumberOfChannels() * input.Width, input.Image);
            switch (runningMode)
            {
                case Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM:
                    // outputInputLookup[input.Timestamp] = input.Image;
                    graph.DetectAsync(img, input.Timestamp);
                    var img_copy = new NativeArray<byte>(input.Image, Allocator.Persistent);
                    outputInputLookup[input.Timestamp] = img_copy;
                    break;
            
                case Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE:
                    var result = graph.Detect(img);
                    // Trigger callback using inherited method
                    TriggerCallbacks(new MPHandsOutput(input.Image, result));
                    break;
            
                case Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO:
                    var videoResult = graph.DetectForVideo(img, input.Timestamp);
                    // Trigger callback using inherited method
                    TriggerCallbacks(new MPHandsOutput(input.Image, videoResult));
                    break;
            }
        }
    }

    public record ClassPredictions(
        List<string> Classes,
        float[] Probabilities
    );

    public record PopsignIsolatedSLRInput(List<HandLandmarkerResult> Result);
    public class LiteRTPopsignIsolatedSLR : CallbackManager<ClassPredictions>, IModel<PopsignIsolatedSLRInput>
    {
        private Interpreter interpreter;
        private List<string> mapping;
        
		
        private readonly float[] outputs = new float[563];
        private float[] inputs = new float[42 * 60];

        public LiteRTPopsignIsolatedSLR(byte[] modelAssetBuffer, List<string> mapping)
        {
            this.mapping = mapping;
            var options = new InterpreterOptions();
            options.threads = 1;
            interpreter = new Interpreter(modelAssetBuffer, options);
            interpreter.AllocateTensors();
        }

        public void Run(PopsignIsolatedSLRInput input) {
            var data = GetInputArray(input);
            Array.Copy(data, inputs, data.Length);
            interpreter.SetInputTensorData(0, inputs);
            // Debug.Log("invoke");
            interpreter.Invoke();

            // interpreter.GetOutputTensorData(0, modelOutputTensor.AsSpan());
			
            interpreter.GetOutputTensorData(0, outputs);
            float[] sendOutputs = new float[outputs.Length];
            Array.Copy(outputs, sendOutputs, outputs.Length);
            
            TriggerCallbacks(new ClassPredictions(mapping.ToList(), outputs.ToArray()));
        }

        private float[] GetInputArray(PopsignIsolatedSLRInput input)
        {
            // This is where you would structure the input to fit the model. For now, it's just a placeholder.
            return new float[42 * 60];
        }
    }


}