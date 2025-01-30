using SLRGTk.Common;
using SLRGTk.Engine;
using SLRGTk.Preview;
using UnityEngine;
using UnityEngine.UI;
public class MainScript : MonoBehaviour
{
    private LiteRTTestEngine engine;
    [SerializeField] private RawImage image;
    [SerializeField] private HandLandmarkAnnotator _annotator;

    // Example usage for testing:
    private void Awake() {
        engine = gameObject.AddComponent<LiteRTTestEngine>();
        engine.mp.AddCallback("ImagePreview", output => {
            _annotator.UpdateLandmarks(output.Result);
            _annotator.UpdateImage(output.OriginalImage);
        });
        _annotator.Show();
    }
}
