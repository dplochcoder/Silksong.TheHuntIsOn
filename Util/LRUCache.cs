using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Silksong.TheHuntIsOn.Util;

public delegate bool LRUCacheLoader<K, V>(K key, out V value);

internal class LRUCache<K, V>(int Size, LRUCacheLoader<K, V> Loader)
{
    private record Entry(K Key, V Value) { }

    private readonly LinkedList<Entry> cacheList = [];  // FIFO
    private readonly Dictionary<K, LinkedListNode<Entry>> cacheDict = [];
    private readonly HashSet<K> negativeCache = [];

    public bool TryGetValue(K key, [MaybeNullWhen(false)] out V value)
    {
        if (negativeCache.Contains(key))
        {
            value = default;
            return false;
        }

        if (cacheDict.TryGetValue(key, out var node))
        {
            cacheList.Remove(node);
            cacheList.AddLast(node);

            value = node.Value.Value;
            return true;
        }

        if (Loader(key, out value))
        {
            node = cacheList.AddLast(new Entry(key, value));
            cacheDict.Add(key, node);

            if (cacheList.Count > Size)
            {
                var first = cacheList.First;
                cacheList.RemoveFirst();
                cacheDict.Remove(first.Value.Key);
            }
            return true;
        }
        else
        {
            negativeCache.Add(key);
            value = default;
            return false;
        }
    }

    public void Evict(K key)
    {
        if (negativeCache.Remove(key)) return;

        if (cacheDict.TryGetValue(key, out var node))
        {
            cacheDict.Remove(key);
            cacheList.Remove(node);
        }
    }

    public void Clear()
    {
        cacheList.Clear();
        cacheDict.Clear();
        negativeCache.Clear();
    }
}
