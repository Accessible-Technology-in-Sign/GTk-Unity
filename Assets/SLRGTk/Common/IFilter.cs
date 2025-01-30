namespace SLRGTk.Common {
    using System;
    using System.Collections.Generic;

    public interface IFilter<T>
    {
        void AddFilter(string name, Func<T, T> filter);
        void RemoveFilter(string name);
        T Filter(T value);
        void ClearCallbacks();
    }

    public class FilterManager<T> : IFilter<T>
    {
        private readonly Dictionary<string, Func<T, T>> _filters = new Dictionary<string, Func<T, T>>();

        public void AddFilter(string name, Func<T, T> filter)
        {
            _filters[name] = filter;
        }

        public void RemoveFilter(string name)
        {
            _filters.Remove(name);
        }

        public T Filter(T value)
        {
            T currentValue = value;
            foreach (var filter in _filters.Values)
            {
                currentValue = filter(currentValue);
            }
            return currentValue;
        }

        public void ClearCallbacks()
        {
            _filters.Clear();
        }
    }

}