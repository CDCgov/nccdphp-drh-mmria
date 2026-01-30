using System;
using System.Collections.Generic;
using Unit = System.ValueTuple;

namespace mmria.common.functional
{
    using static F;

    public static partial class F
    {
        public static Unit Unit() => default(Unit);
    }

    public static class ActionExt
    {
        public static Func<Unit> ToFunc(this Action action) => () => { action(); return Unit(); };
        public static Func<T, Unit> ToFunc<T>(this Action<T> action) => (t) => { action(t); return Unit(); };
    
    
        public static Option<T> Some<T>(T value) => new Option.Some<T>(value);
        public static Option.None None => Option.None.Default;
    
    }


    public struct Age
    {
        private int Value { get; }
        public static Option<Age> Of(int age)=> IsValid(age) ? new Option.Some<Age>(new Age(age)) : Option.None.Default;

        private Age(int value)
        {
            if (!IsValid(value))
                throw new ArgumentException($"{value} is not a valid age");
            Value = value;
        }
        private static bool IsValid(int age)=> 0 <= age && age < 120;
    }



    namespace Option
    {
        public struct None
        {
            internal static readonly None Default = new None();
        }

        public struct Some<T>
        {
            internal T Value { get; }

            internal Some(T value)
            {
                if (value == null) throw new ArgumentNullException();
                Value = value;
            }
        }
    }
    public struct Option<T> : IEquatable<Option.None>, IEquatable<Option<T>>
    {
        readonly bool isSome;
        bool isNone => !isSome;
        readonly T value;

        private Option(T value)
        {
            this.isSome = true;
            this.value = value;
        }

        public static implicit operator Option<T>(Option.None _) => new Option<T>();
        public static implicit operator Option<T>(Option.Some<T> some) => new Option<T>(some.Value);

        public static implicit operator Option<T>(T value) => value == null ? Option.None.Default : new Option.Some<T>(value);
        public R Match<R>(Func<R> None, Func<T, R> Some) => isSome ? Some(value) : None();

        public IEnumerable<T> AsEnumerable()
        {
            if (isSome) yield return value;
        }

        public bool Equals(Option.None _) => isNone;

        public bool Equals(Option<T> other) 
            => this.isSome == other.isSome 
            && (this.isNone || this.value.Equals(other.value));

/*
        public override int GetHashCode()
        {
            int hash = 17;

            if(value != null)
            {
                unchecked // Overflow is fine, just wrap
                {
                    // Suitable nullity checks etc, of course :)
                    hash = hash * 23 + value. GetHashCode();
                    return hash;
                }
            }
            else
            {
                return hash;
            }
        }
*/
        public static bool operator ==(Option<T> @this, Option<T> other) => @this.Equals(other);
        public static bool operator !=(Option<T> @this, Option<T> other) => !(@this == other);

        public override string ToString() => isSome ? $"Some({value})" : "None";

    }

    public static class OptionExt
    {
        public static Option<string> Lookup(this System.Collections.Specialized.NameValueCollection @this, string key) => @this[key];

        public static Option<T> Lookup<K, T>(this IDictionary<K, T> dict, K key)
        {
            T value;
            return dict.TryGetValue(key, out value)? new Option.Some<T>(value) : Option.None.Default;
        }

    }
  
}