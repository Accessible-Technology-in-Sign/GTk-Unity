using System.Collections.Concurrent;
using UnityEngine;

namespace SLRGTk.Common {
    public class CustomTextureManager : MonoBehaviour {
        private static readonly ConcurrentQueue<Texture> TextureBin = new();
        public static void ScheduleDeletion(Texture toDelete) {
            TextureBin.Enqueue(toDelete);
        }

        public static void DeleteTextures() {
            while (TextureBin.TryDequeue(out var texture) && texture) {
                DeleteNow(texture);
            }
        }
        public static void DeleteNow(Texture toDelete) {
            toDelete.hideFlags = HideFlags.None;
            Destroy(toDelete);
        }
    }
}