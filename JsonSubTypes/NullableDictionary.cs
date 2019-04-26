using System;
using System.Collections.Generic;

namespace JsonSubTypes
{
    public class NullableDictionary<TKey, TValue> //: IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private bool _hasNullKey = false;
        private TValue _nullKeyValue;
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        public bool TryGetValue(TKey lookupValue, out TValue type)
        {
            if (Equals(lookupValue, default(TKey)))
            {
                if (!_hasNullKey)
                {
                    type = default(TValue);
                    return false;
                }

                type = _nullKeyValue;
                return true;
            }

            return _dictionary.TryGetValue(lookupValue, out type);
        }

        public void Add(TKey objAssociatedValue, TValue objSubType)
        {
            if (Equals(objAssociatedValue, default(TKey)))
            {
                if (_hasNullKey)
                {
                    throw new ArgumentException();
                }

                _hasNullKey = true;
                _nullKeyValue = objSubType;
            }
            else
            {
                _dictionary.Add(objAssociatedValue, objSubType);
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
