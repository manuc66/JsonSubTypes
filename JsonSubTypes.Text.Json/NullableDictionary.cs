using System;
using System.Collections.Generic;

namespace JsonSubTypes.Text.Json;

internal class NullableDictionary<TKey, TValue> where TKey : notnull
{
    private bool _hasNullKey;
    private TValue? _nullKeyValue;
    private readonly Dictionary<TKey, TValue> _dictionary = new();

    public bool TryGetValue(TKey? key, out TValue? value)
    {
        if (key is null)
        {
            if (!_hasNullKey)
            {
                value = default;
                return false;
            }

            value = _nullKeyValue;
            return true;
        }

        return _dictionary.TryGetValue(key, out value);
    }

    public void Add(TKey? key, TValue value)
    {
        if (key is null)
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

    public void Set(TKey? key, TValue value)
    {
        if (key is null)
        {
            _hasNullKey = true;
            _nullKeyValue = value;
        }
        else
        {
            _dictionary[key] = value;
        }
    }

    public IEnumerable<TKey> NotNullKeys()
    {
        return _dictionary.Keys;
    }

    public IEnumerable<KeyValuePair<TKey?, TValue>> Entries()
    {
        if (_hasNullKey)
        {
            yield return new KeyValuePair<TKey?, TValue>(default, _nullKeyValue!);
        }

        foreach (KeyValuePair<TKey, TValue> value in _dictionary)
        {
            yield return new KeyValuePair<TKey?, TValue>(value.Key, value.Value);
        }
    }
}
