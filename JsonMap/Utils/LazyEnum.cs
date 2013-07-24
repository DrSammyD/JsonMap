/* Copyright 2013 MacReport Media Publishing Inc.
 * Licensed under MPL-2.0 (see /LICENSE)
 * If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * 
 * Author: Sam Armstrong
 */

using System;

namespace JsonMap
{
    public sealed class LazyEnum : IComparable<Enum>
    {
        private Object _value;
        private Type _type;

        public LazyEnum(Object val)
        {
            _value = val;
            if (val is Enum)
            {
                _type = val.GetType();
            }
            else
            {
                _type = typeof(Enum);
            }
        }

        public Object Value
        {
            get
            {
                return this._value;
            }
        }

        public static implicit operator LazyEnum(Int64 e)
        {
            return new LazyEnum(e);
        }

        public static implicit operator Int64(LazyEnum LE)
        {
            return (int)Enum.ToObject(LE._type, LE._value);
        }

        public static implicit operator LazyEnum(string e)
        {
            return new LazyEnum(e);
        }

        public static implicit operator String(LazyEnum LE)
        {
            return (String)Enum.Parse(LE._type, LE._value.ToString());
        }

        public static implicit operator Enum(LazyEnum LE)
        {
            return (Enum)Enum.Parse(LE._type, LE._value.ToString());
        }

        public static implicit operator LazyEnum(Enum e)
        {
            return new LazyEnum(e);
        }

        public LazyEnum SetType(Type type)
        {
            _type = type;
            return this;
        }

        public int CompareTo(Enum other)
        {
            var comp = Enum.Parse(other.GetType(), this._value.ToString());
            if (comp == other) return 0;
            return -1;
        }

        public override bool Equals(object obj)
        {
            if (obj is Enum)
            {
                return Enum.ToObject(obj.GetType(), this._value).Equals(Enum.ToObject(obj.GetType(), obj));
            }
            return base.Equals(obj);
        }

        public static bool operator ==(Enum a, LazyEnum b)
        {
            var obj = a;
            return Enum.ToObject(obj.GetType(), b._value).Equals(Enum.ToObject(obj.GetType(), obj));
        }

        public static bool operator ==(LazyEnum b, Enum a)
        {
            var obj = a;
            return Enum.ToObject(obj.GetType(), b._value).Equals(Enum.ToObject(obj.GetType(), obj));
        }

        public static bool operator !=(Enum a, LazyEnum b)
        {
            return !(a == b);
        }

        public static bool operator !=(LazyEnum b, Enum a)
        {
            return !(a == b);
        }
    }
}