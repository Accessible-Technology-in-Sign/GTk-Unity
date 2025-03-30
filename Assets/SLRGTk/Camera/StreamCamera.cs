using System;
using System.Collections;
using SLRGTk.Common;
using SLRGTk.Model;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace SLRGTk.Camera {
    public class StreamCamera : MonoBehaviour, ICamera, ICallback<MPVisionInput> {
        private static readonly int SwapBR = Shader.PropertyToID("_SwapBR");
        private static readonly int RotationAngle = Shader.PropertyToID("_RotationAngle");
        private static readonly int HorizontalFlip = Shader.PropertyToID("_HorizontalFlip");

        private readonly CallbackManager<MPVisionInput> _callbackManagerProxy = new();

        // Inherit callback manager functionality
        private Material _webcamControlShader;
        //a WebCamTexture essentially is the raw video feed from the device camera
        private WebCamTexture _webCamTexture;
        private WebCamDevice? _currentDevice;
        //temporary storage for pixel data when transferring image between camera and memory
        private Color32[] _textureTransferBuffer;


        public bool polling = true;
        public CameraSelector cameraSelector = CameraSelector.FirstFrontCamera;

        private void Awake() {
            _webcamControlShader = new Material(Shader.Find("Nana/WebcamControlShader"));
        }

        // essentially sets up the camera
        private void UpdateProps() {
            // checks to see if there are any cameras
            if (WebCamTexture.devices.Length <= 0) throw new Exception("Camera not connected");

            // loops through each camera device and assigns _currentDevice accordingly
            foreach (var device in WebCamTexture.devices) {
                switch (cameraSelector) {
                    case CameraSelector.FirstFrontCamera:
                        if (device.isFrontFacing) {
                            _currentDevice = device;
                            goto ISO;
                        }
                        break;

                    case CameraSelector.FirstBackCamera:
                        if (!device.isFrontFacing) {
                            _currentDevice = device;
                            goto ISO;
                        }
                        break;
                }
            }
            // the default value for _currentDevice (wouldn't it be better to instantiate _currentDevice to this first to make sure we don't use goto)
            _currentDevice = WebCamTexture.devices[0]; // Fallback if no match
        ISO:
            // hard setting the webcam to use 720x1280 px and 30 fps
            _webCamTexture = new WebCamTexture(_currentDevice.Value.name, 720, 1280, 30);
            // sets the size of the array to height*width, so it's essentially flattened storage of the pixels
            _textureTransferBuffer = new Color32[_webCamTexture.width * _webCamTexture.height];
            // checks to see if webcamcontrolshader is initialized
            if (_webcamControlShader) {
                if (_webCamTexture.graphicsFormat == GraphicsFormat.R8G8B8A8_UNorm) {
                    // default behavior
                }
                else if (_webCamTexture.graphicsFormat == GraphicsFormat.R8G8B8A8_SRGB) {
                    // default behaviour
                }
                else if (_webCamTexture.graphicsFormat == GraphicsFormat.B8G8R8A8_UNorm) {
                    // Adjust for iOS and OSX webcams
                    _webcamControlShader.SetInt(SwapBR, 0);
                }
                else if (_webCamTexture.graphicsFormat == GraphicsFormat.B8G8R8A8_SRGB) {
                    // Adjust for iOS and OSX webcams
                    Debug.Log(_webcamControlShader);
                    _webcamControlShader.SetInt(SwapBR, 0);
                }
                else {
                    throw new Exception("Unsupported graphics format from webcam: " + _webCamTexture.graphicsFormat);
                }
            }
        }
        //starts the logic to turn on the camera
        public void Poll() {
            // checks to see if the webcam is not initialized
            if (!_webCamTexture) {
                // called to set up the camera
                UpdateProps();
            }
            Debug.Log("Polling");
            // sets the polling on to start receiving images
            polling = true;
            // starts the camera
            _webCamTexture.Play();
        }

        public void Pause() {
            // turns off polling
            polling = false;
            // pauses the camera
            _webCamTexture.Pause();
        }

        private IEnumerator Run() {
            // // Debug.Log("debug Updating");
            // if (polling) {
            //     // Debug.Log("debug Polling");
            //     // if (!_webCamTexture || !_webCamTexture.isPlaying) {
            //     //     Poll();
            //     // }
            //     if (_webCamTexture.didUpdateThisFrame && _webCamTexture.width > 0 && _webCamTexture.height > 0) {
            //         Debug.Log("debug Webcam");
            //         _webcamControlShader.SetFloat(RotationAngle, _webCamTexture.videoRotationAngle);
            //         _webcamControlShader.SetInt(HorizontalFlip, _webCamTexture.videoVerticallyMirrored ? 1 : 0);
            //
            //         RenderTexture tempRT = RenderTexture.GetTemporary(
            //             _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.width : _webCamTexture.height,
            //             _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.height : _webCamTexture.width,
            //             0, GraphicsFormatUtility.GetGraphicsFormat(TextureFormat.RGBA32, true));
            //
            //         Graphics.Blit(_webCamTexture, tempRT, _webcamControlShader);
            //
            //         RenderTexture.active = tempRT;
            //         
            //         foreach (var callback in _callbackManagerProxy.callbacks) {
            //             var dest = new Texture2D(
            //                 _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.width : _webCamTexture.height,
            //                 _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.height : _webCamTexture.width,
            //                 TextureFormat.RGBA32,
            //                 false, false, true); //TODO: check srgb vs linear
            //             Graphics.CopyTexture(dest, tempRT);
            //             // dest.ReadPixels(new Rect(0, 0, _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.width : _webCamTexture.height,
            //             //     _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.height : _webCamTexture.width), 0, 0, false);
            //             // dest.Apply();
            //             // _webCamTexture.GetPixels32(_textureTransferBuffer);
            //             // dest.SetPixels32(_textureTransferBuffer);
            //             // dest.Apply();
            //             callback.Value(dest);
            //         }
            //         RenderTexture.active = null;
            //         RenderTexture.ReleaseTemporary(tempRT);
            //     }
            // } else {
            //     // if (_webCamTexture && _webCamTexture.isPlaying) {
            //     //     Pause();
            //     // }
            // }

            // polling is essentially when we constantly check to see if there are any new frames coming into the camera
            if (polling) {
                // checks to see if the camera is not instantiated or if it is not playing
                if(_webCamTexture == null || !_webCamTexture.isPlaying) {
                    // starts up the camera and sets it up in code with UpdateProps
                    Poll();
                }
                // checks to see if we got a new image
                if (_webCamTexture.didUpdateThisFrame && _webCamTexture.width > 0 && _webCamTexture.height > 0) {
                    // temporary workspace for the image processing
                    RenderTexture tempRT = RenderTexture.GetTemporary(
                        _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.width : _webCamTexture.height, // webCamTexture.width, 
                        _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.height : _webCamTexture.width, // webCamTexture.height, 
                        0, GraphicsFormatUtility.GetGraphicsFormat(TextureFormat.RGBA32, true));
                    // copies the current image from _webCamTexture to the tempRT after applying the webcamControlShader
                    Graphics.Blit(_webCamTexture, tempRT, _webcamControlShader);
                    // essentially going through all the callbacks and calling them on an image
                    var req = AsyncGPUReadback.Request(tempRT, 0, TextureFormat.RGBA32, (AsyncGPUReadbackRequest request) => {
                        // var dest = new Texture2D(
                        //     _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.width : _webCamTexture.height, // webCamTexture.width, 
                        //     _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.height : _webCamTexture.width, // webCamTexture.height, 
                        //     TextureFormat.RGBA32,
                        //     false);                        
                        // checks through each callback
                        foreach (var callback in _callbackManagerProxy.callbacks) {
                            // checks to see if GPU is giving some kind of error
                            if (request.hasError)
                                Debug.LogError("GPU readback error.");
                            // calls the callback on the image
                            // TODO: synchronize in a queue
                            callback.Value(new MPVisionInput(request.GetData<byte>(), DateTimeOffset.Now.ToUnixTimeMilliseconds(), _webCamTexture.width, _webCamTexture.height));
                        }
                        // releasing the temporary workspace to be used in the next run through
                        RenderTexture.ReleaseTemporary(tempRT);
                    });
                    // pauses the execution until we get the value of req is set and then returns
                    yield return req;

                    // foreach (var callback in _callbackManagerProxy.callbacks) {
                    //     // Why create a new texture for each callback? because memory management - if one callback frees the
                    //     // texture I don't want it to affect another. 
                    //     // Ideally a reference counter should do the trick and allow for much efficient operation - can 
                    //     // look into that with a custom class to manage the resource instead of passing around Texture2D
                    //     // TODO: Reference Counter
                    //     _webcamControlShader.SetFloat(RotationAngle, _webCamTexture.videoRotationAngle);
                    //     _webcamControlShader.SetInt(HorizontalFlip, _webCamTexture.videoVerticallyMirrored ? 1 : 0);
                    //     // Debug.Log($"Webcam rotation: {webCamTexture.videoRotationAngle}");
                    //     // Debug.Log($"Webcam resolution: {webCamTexture.width}x{webCamTexture.height}");
                    //     // TODO: Figure out a way to rotate the entire texture including the dimensions rather than just the UVs
                    //     var dest = new Texture2D(
                    //         _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.width : _webCamTexture.height, // webCamTexture.width, 
                    //         _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.height : _webCamTexture.width, // webCamTexture.height, 
                    //         TextureFormat.RGBA32,
                    //         false);
                    //     
                    //
                    //     RenderTexture tempRT = RenderTexture.GetTemporary(
                    //         _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.width : _webCamTexture.height, // webCamTexture.width, 
                    //         _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.height : _webCamTexture.width, // webCamTexture.height, 
                    //         0, GraphicsFormatUtility.GetGraphicsFormat(TextureFormat.RGBA32, true));
                    //     // _webcamControlShader.SetTexture("_MainTex", webCamTexture);
                    //     Graphics.Blit(_webCamTexture, tempRT, _webcamControlShader);
                    //
                    //     ////////////////////////////////////////////////
                    //     ///// This does not seem to be working - seems like textures are left in GPU and all GPU
                    //     ///// Pipelines ( the preview afterwards) will handle it okay, but the pixels are not CPU
                    //     ///// readable and Mediapipe doesnt really play with that.
                    //     ////////////////////////////////////////////////
                    //     ////////////////===OLD COMMENT==////////////////
                    //     // The tempRT is required on Android and iOS since the webcam texture is not on the GPU then and the
                    //     // GL pipelines they have don't allow for a one line copy texture.
                    //     // Theoretically in the editor - i was able to get away with just CopyTexture but on the mobile
                    //     // devices it crashes since the webcamtexture is not on the GPU which CopyTexture requires.
                    //     ////////////////======CODE======////////////////
                    //     // Graphics.CopyTexture(tempRT, dest);
                    //     ////////////////////////////////////////////////
                    //
                    //     // RenderTexture.active = tempRT;
                    //     // dest.ReadPixels(new Rect(
                    //     //     0, 0, 
                    //     //     _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.width : _webCamTexture.height, // webCamTexture.width, 
                    //     //     _webCamTexture.videoRotationAngle % 180 == 0 ? _webCamTexture.height : _webCamTexture.width // webCamTexture.height
                    //     //     ), 0, 0, false);
                    //     // dest.Apply();
                    //     // RenderTexture.active = null;
                    //
                    //     RenderTexture.ReleaseTemporary(tempRT);
                    //     callback.Value(dest);
                    // }
                }
            }
            else {
                // pauses the camera if we aren't polling/looking for more images right now
                if (_webCamTexture  && _webCamTexture.isPlaying)
                    Pause();
            }
        }

        public void Update() {
            StartCoroutine(Run());
        }

        public void AddCallback(string callbackName, Action<MPVisionInput> callback) {
            Debug.Log("Adding Callback");
            _callbackManagerProxy.AddCallback(callbackName, callback);
        }
        public void RemoveCallback(string callbackName) {
            _callbackManagerProxy.RemoveCallback(callbackName);
        }
        public void TriggerCallbacks(MPVisionInput value) {
            _callbackManagerProxy.TriggerCallbacks(value);
        }
        public void ClearCallbacks() {
            _callbackManagerProxy.ClearCallbacks();
        }
    }
}