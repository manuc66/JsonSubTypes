using System;
using System.Collections.Generic;

namespace JsonSubTypes
{
    public class NullableDictionary<TKey, TValue>
    {
        private bool _hasNullKey;
        private TValue _nullKeyValue;
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (Equals(key, default(TKey)))
            {
                if (!_hasNullKey)
                {
                    value = default(TValue);
                    return false;
                }

                value = _nullKeyValue;
                return true;
            }

            return _dictionary.TryGetValue(key, out value);
        }

        public void Add(TKey key, TValue value)
        {
            if (Equals(key, default(TKey)))
            {
                if (_hasNullKey)
                {
                    throw new ArgumentException();
                }

                _hasNullKey = true;
                _nullKeyValue = value;
            }
            else
            {
                _dictionary.Add(key, value);
            }
        }

        public IEnumerable<TKey> NotNullKeys()
        {
            return _dictionary.Keys;
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> Entries()
        {
            if (_hasNullKey)
            {
                yield return new KeyValuePair<TKey, TValue>(default(TKey), _nullKeyValue);
            }

            foreach (var value in _dictionary)
            {
                yield return value;
            }
        }
    }
}
