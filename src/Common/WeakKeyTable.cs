using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WotrClassMod.Common
{
    // For identity records without an end callback. Values must not own their keys, even indirectly.
    // Game Mono's weak handles work, but its ConditionalWeakTable retains keys (WeakTableLifetimeTests).
    internal sealed class WeakKeyTable<TKey, TValue> where TKey : class where TValue : class
    {
        sealed class Key : WeakReference
        {
            internal readonly int Hash;
            internal Key(TKey key) : base(key) { Hash = RuntimeHelpers.GetHashCode(key); }
        }

        // Stored keys are private weak handles. Lookups use the caller's object without allocation.
        sealed class Identity : IEqualityComparer<object>
        {
            public new bool Equals(object a, object b)
            {
                if (ReferenceEquals(a, b)) return true;
                object left = a is Key x ? x.Target : a;
                object right = b is Key y ? y.Target : b;
                return left != null && ReferenceEquals(left, right);
            }

            public int GetHashCode(object key)
            {
                return key is Key stored ? stored.Hash : RuntimeHelpers.GetHashCode(key);
            }
        }

        readonly Dictionary<object, TValue> entries = new Dictionary<object, TValue>(new Identity());
        int lastCollection = GC.CollectionCount(0);

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (entries) return entries.TryGetValue(key, out value);
        }

        public bool Remove(TKey key)
        {
            lock (entries) return entries.Remove(key);
        }

        public void Add(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException("key");
            lock (entries) Insert(key, value);
        }

        public TValue GetValue(TKey key, Func<TKey, TValue> create)
        {
            lock (entries)
            {
                TValue value;
                if (entries.TryGetValue(key, out value)) return value;
                value = create(key);
                Insert(key, value);
                return value;
            }
        }

        void Insert(TKey key, TValue value)
        {
            // Only a collection can invalidate a weak handle. Sweep on the next insertion.
            int collection = GC.CollectionCount(0);
            if (collection != lastCollection)
            {
                List<Key> dead = null;
                foreach (Key stored in entries.Keys)
                {
                    if (stored.IsAlive) continue;
                    if (dead == null) dead = new List<Key>();
                    dead.Add(stored);
                }
                if (dead != null) foreach (var stored in dead) entries.Remove(stored);
                lastCollection = collection;
            }
            entries.Add(new Key(key), value);
            GC.KeepAlive(key);
        }
    }
}
