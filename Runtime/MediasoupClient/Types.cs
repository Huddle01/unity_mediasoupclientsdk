using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mediasoup.Types
{
    public class Types
    {

    }

    public class AppData : IDictionary
    {
        private readonly Dictionary<object, object> _data = new Dictionary<object, object>();

        public object this[object key]
        {
            get => _data[key];
            set => _data[key] = value;
        }

        public bool IsFixedSize => false;
        public bool IsReadOnly => false;
        public ICollection Keys => _data.Keys;
        public ICollection Values => _data.Values;
        public int Count => _data.Count;
        public bool IsSynchronized => false;
        public object SyncRoot => new object();

        public void Add(object key, object value)
        {
            _data.Add(key, value);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(object key)
        {
            return _data.ContainsKey(key);
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)_data).CopyTo(array, index);
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        public void Remove(object key)
        {
            _data.Remove(key);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }
}