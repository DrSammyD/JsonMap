/* Copyright 2013 MacReport Media Publishing Inc.
 * Licensed under MPL-2.0 (see /LICENSE)
 * If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * 
 * Author: Sam Armstrong
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonMap
{
    public class LambdaComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _lambdaComparer;
        private readonly Func<T, int> _lambdaHash;

        public LambdaComparer(Func<T, T, bool> lambdaComparer) :
            this(lambdaComparer, o => 0)
        {
        }

        public LambdaComparer(Func<T, T, bool> lambdaComparer, Func<T, int> lambdaHash)
        {
            if (lambdaComparer == null)
                throw new ArgumentNullException("lambdaComparer");
            if (lambdaHash == null)
                throw new ArgumentNullException("lambdaHash");

            _lambdaComparer = lambdaComparer;
            _lambdaHash = lambdaHash;
        }

        public bool Equals(T x, T y)
        {
            return _lambdaComparer(x, y);
        }

        public int GetHashCode(T obj)
        {
            return _lambdaHash(obj);
        }
    }

    public static class Ext
    {
        public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first,
            IEnumerable<TSource> second, Func<TSource, TSource, bool> comparer)
        {
            return first.Except(second, new LambdaComparer<TSource>(comparer));
        }

        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> enumerable, Func<TSource, TSource, bool> comparer)
        {
            return enumerable.Distinct(new LambdaComparer<TSource>(comparer));
        }
    }

    public class InheritanceComparer : Comparer<Type>
    {
        public override int Compare(Type x, Type y)
        {
            if (x.IsSubclassOf(y)) return 1;
            if (y.IsSubclassOf(x)) return -1;
            return 0;
        }
    }

    public class JsonMapTypeEnumComparer : Comparer<Enum>
    {
        private Enum _preference;

        public JsonMapTypeEnumComparer(Enum preference)
            : base()
        {
            _preference = preference;
        }

        public override int Compare(Enum x, Enum y)
        {
            if (x.CompareTo(y) == 0) return 0;
            if (x.CompareTo(_preference) == 0) return -1;
            if (y.CompareTo(_preference) == 0) return 1;
            if (x.CompareTo(JTransformer.Queryer.DefaultMap) == 0) return -1;
            if (y.CompareTo(JTransformer.Queryer.DefaultMap) == 0) return 1;

            return x.CompareTo(y);
        }
    }
}