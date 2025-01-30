using System;
using System.Collections.Generic;

namespace SLRGTk.Common {
    public interface ICallback<T> {
        void AddCallback(string callbackName, Action<T> callback);
        void RemoveCallback(string callbackName);
        void TriggerCallbacks(T value);
        
        void ClearCallbacks();
    }

    public class CallbackManager<T> : ICallback<T> {
        public readonly Dictionary<string, Action<T>> callbacks = new ();

        public void AddCallback(string callbackName, Action<T> callback) {
            callbacks[callbackName] = callback;
        }

        public void RemoveCallback(string callbackName) {
            callbacks.Remove(callbackName);
        }

        public void TriggerCallbacks(T value) {
            foreach (var callback in callbacks.Values) {
                callback(value);
            }
        }

        public void ClearCallbacks() {
            callbacks.Clear();
        }
    }
}