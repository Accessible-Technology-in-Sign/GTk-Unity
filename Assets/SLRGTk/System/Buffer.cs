using System;
using System.Collections.Generic;
using System.Linq;
using SLRGTk.Common;

public class DefaultFillerDefinitions<T>
{
    public Func<List<T>, T, bool> PassThroughFiller()
    {
        return (internalList, frame) =>
        {
            internalList.Add(frame);
            return true;
        };
    }

    public Func<List<T>, T, bool> CapacityFiller(int capacity) {
        return (internaList, frame) => {
            internaList.Add(frame);
            var ret = internaList.Count >= capacity;
            while (internaList.Count > capacity) internaList.RemoveAt(internaList.Count - 1);
            return ret;
        };
    }
}

public class Buffer<T> : CallbackManager<List<T>>
{
    private readonly List<T> internalBuffer = new List<T>();
    public Func<List<T>, T, bool> Filler { get; set; }

    public Buffer()
    {
        Filler = new DefaultFillerDefinitions<T>().CapacityFiller(60);
    }

    public void AddElement(T elem)
    {
        if (Filler(internalBuffer, elem))
        {
            TriggerCallbacks();
        }
    }

    public void TriggerCallbacks()
    {
        TriggerCallbacks(internalBuffer.ToList());
    }

    public void Clear()
    {
        internalBuffer.Clear();
    }

    public int Size => internalBuffer.Count;
}
