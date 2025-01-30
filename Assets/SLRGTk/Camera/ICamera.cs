namespace SLRGTk.Camera {
    public interface ICamera {
        void Poll();
        void Pause();
    }

    public enum CameraSelector {
        FirstFrontCamera,
        FirstBackCamera
    }
}
