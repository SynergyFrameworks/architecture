using System;
using System.Collections.Generic;
using System.Linq;

namespace MML.Enterprise.Common.DataStructures
{
    public class SequentialDictionary<T, R>
    {
        public List<KeyValuePair<T, R>> Collection { get; set; }

        public SequentialDictionary()
        {
            Collection = new List<KeyValuePair<T, R>>();
        }

        public SequentialDictionary(T key, R value)
        {
            Collection = new List<KeyValuePair<T, R>> { new KeyValuePair<T, R>(key, value) };
        }

        public bool ContainsKey(T key)
        {
            return Collection != null && Collection.Any(kvp => kvp.Key.Equals(key));
        }

        public KeyValuePair<T, R> this[T key]
        {
            get { return Collection.FirstOrDefault(c => c.Key.Equals(key)); }
            set
            {
                var index = Collection.FindIndex(c => c.Key.Equals(key));
                Collection.RemoveAt(index);
                Collection.Insert(index, value);
            }
        }

        public bool Remove(T key)
        {
            if (!ContainsKey(key))
                return false;

            var index = Collection.FindIndex(c => c.Key.Equals(key));
            Collection.RemoveAt(index);
            return true;
        }

        public void Add(T key, R value)
        {
            EnsureInitialized();
            Collection.Add(new KeyValuePair<T, R>(key, value));
        }

        public bool Any()
        {
            return Collection != null && Collection.Any();
        }

        public List<T> Keys
        {
            get
            {
                return Collection.Select(c => c.Key).ToList();
            }
        }

        private void EnsureInitialized()
        {
            if (Collection == null)
                Collection = new List<KeyValuePair<T, R>>();
        }
    }
}
