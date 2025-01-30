using Mediapipe.Tasks.Vision.HandLandmarker;
using SLRGTk.Common;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SLRGTk.Preview {
    public enum PainterMode {
        ImageOnly = 0,
        SkeletonOnly = 1,
        ImageAndSkeleton = 2
    }
    public class HandLandmarkAnnotator: MonoBehaviour {
        
        public PainterMode painterMode = PainterMode.ImageAndSkeleton;

        [SerializeField]
        private RawImage screen;

        [SerializeField] private bool preserveAR = false;

        private RenderTexture _rt;

        private HandLandmarkerResult? _result;
        private Texture2D _image;
        private NativeArray<byte> _lastNative;
        
        public bool Visible { get; private set; }
        
        private Material _graphMaterial;
        public float pointRadius = 0.000005f;
        public float strokeWidth = 0.000005f;
        public Color pointColor = Color.red;
        public Color lineColor = Color.blue;

        private static readonly int LandmarksPresent = Shader.PropertyToID("_LandmarksPresent");
        private static readonly int PointColor = Shader.PropertyToID("_PointColor");
        private static readonly int LineColor = Shader.PropertyToID("_LineColor");
        private static readonly int Radius = Shader.PropertyToID("_Radius");
        private static readonly int Points = Shader.PropertyToID("_Points");
        private static readonly int StrokeWidth = Shader.PropertyToID("_StrokeWidth");
        private static readonly int DrawingMode = Shader.PropertyToID("_DrawingMode");
        private static readonly int Dims = Shader.PropertyToID("_Dims");

        private void Awake() {
            _graphMaterial = new Material(Shader.Find("Nana/HandLandmarkAnnotator"));
            _image = new(720, 1280, TextureFormat.RGBA32, false);
        }

        public void Hide() {
            Visible = false;
        }
        
        public void Show() {
            Visible = true;
        }
        
        
        public void UpdateLandmarks(HandLandmarkerResult? result) {
            _result = result;
        }
        
        public void UpdateImage(NativeArray<byte> image, bool freeOnUse = true) {
            // if (freeOnUse) CustomTextureManager.ScheduleDeletion(_image);
            // _image = image;
            _lastNative.Dispose();
            _lastNative = image;
        }
        
        void Update() {
            if (!Visible) {
                screen.enabled = false;
            }
            else {
                if (!screen.enabled) {
                    screen.enabled = true;
                }
                if (_image) {
                    if (!_rt || _image.width != _rt.width || _image.height != _rt.height) {
                        if (_rt) CustomTextureManager.ScheduleDeletion(_rt);
                        _rt = new RenderTexture(_image.width, _image.height, 0);
                        _rt.Create();
                        screen.texture = _rt;
                    } 
                    
                    Debug.Log(_lastNative.Length);
                    if (!(_lastNative.IsCreated)) return;
                    _image.LoadRawTextureData(_lastNative);
                    _image.Apply();
                    if (_result is { handLandmarks: not null } && _result.Value.handLandmarks.Count > 0 && _result.Value.handLandmarks[0] is {landmarks: not null} && _result.Value.handLandmarks[0].landmarks.Count > 0) {
                        var landmarks = _result.Value.handLandmarks[0].landmarks;

                        Vector4[] points = new Vector4[landmarks.Count];
                        
                        for (int i = 0; i < landmarks.Count && i < 21; i++) {
                            points[i] = new Vector2(landmarks[i].x, landmarks[i].y);
                        }
                        
                        _graphMaterial.SetInt(LandmarksPresent, 1);
                        _graphMaterial.SetFloat(Radius, pointRadius);
                        _graphMaterial.SetVectorArray(Points, points);
                        _graphMaterial.SetFloat(StrokeWidth, strokeWidth);
                        _graphMaterial.SetColor(PointColor, pointColor);
                        _graphMaterial.SetColor(LineColor, lineColor);
                    }
                    else {
                        _graphMaterial.SetInt(LandmarksPresent, 0);
                    }
                    _graphMaterial.SetInt(DrawingMode, (int) painterMode);
                    _graphMaterial.SetColor(Dims, new (_image.width, _image.height, screen.rectTransform.rect.width, screen.rectTransform.rect.height));
                    Graphics.Blit(_image, _rt, _graphMaterial);
                }
            }
        }

        private void FixedUpdate() {
            CustomTextureManager.DeleteTextures();
        }

        private void OnDisable() {
            CustomTextureManager.DeleteTextures();
        }
    }
}