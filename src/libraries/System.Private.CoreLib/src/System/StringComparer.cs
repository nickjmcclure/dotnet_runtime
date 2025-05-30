// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract class StringComparer : IComparer, IEqualityComparer, IComparer<string?>, IEqualityComparer<string?>
    {
        public static StringComparer InvariantCulture => CultureAwareComparer.InvariantCaseSensitiveInstance;

        public static StringComparer InvariantCultureIgnoreCase => CultureAwareComparer.InvariantIgnoreCaseInstance;

        public static StringComparer CurrentCulture =>
            new CultureAwareComparer(CultureInfo.CurrentCulture, CompareOptions.None);

        public static StringComparer CurrentCultureIgnoreCase =>
            new CultureAwareComparer(CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);

        public static StringComparer Ordinal => OrdinalCaseSensitiveComparer.Instance;

        public static StringComparer OrdinalIgnoreCase => OrdinalIgnoreCaseComparer.Instance;

        // Convert a StringComparison to a StringComparer
        public static StringComparer FromComparison(StringComparison comparisonType)
        {
            return comparisonType switch
            {
                StringComparison.CurrentCulture => CurrentCulture,
                StringComparison.CurrentCultureIgnoreCase => CurrentCultureIgnoreCase,
                StringComparison.InvariantCulture => InvariantCulture,
                StringComparison.InvariantCultureIgnoreCase => InvariantCultureIgnoreCase,
                StringComparison.Ordinal => Ordinal,
                StringComparison.OrdinalIgnoreCase => OrdinalIgnoreCase,
                _ => throw new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType)),
            };
        }

        public static StringComparer Create(CultureInfo culture, bool ignoreCase)
        {
            ArgumentNullException.ThrowIfNull(culture);

            return new CultureAwareComparer(culture, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
        }

        public static StringComparer Create(CultureInfo culture, CompareOptions options)
        {
            ArgumentNullException.ThrowIfNull(culture);

            return new CultureAwareComparer(culture, options);
        }

        /// <summary>
        /// Determines whether the specified <see cref="IEqualityComparer{String}"/> is a well-known ordinal string comparer.
        /// </summary>
        /// <param name="comparer">The comparer to query.</param>
        /// <param name="ignoreCase">When this method returns, contains a value stating whether <paramref name="comparer"/>
        /// is case-insensitive. Set to <see langword="false"/> if this method returns <see langword="false"/>.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="comparer"/> is a well-known ordinal string comparer;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// A "well-known ordinal comparer" describes a comparer which behaves identically to <see cref="Ordinal"/>
        /// when passed to <see cref="Dictionary{String, TValue}.Dictionary"/> or <see cref="HashSet{String}.HashSet"/>.
        /// For example, <see cref="EqualityComparer{String}.Default"/> is a well-known ordinal comparer because
        /// a <see cref="Dictionary{String, TValue}"/> given <see cref="EqualityComparer{String}.Default"/> as a constructor
        /// argument will behave identically to a <see cref="Dictionary{String, TValue}"/> given <see cref="Ordinal"/>
        /// as a constructor argument. If <paramref name="ignoreCase"/> is <see langword="true"/> on method exit,
        /// then <paramref name="comparer"/> behaves identically to <see cref="OrdinalIgnoreCase"/> when passed to the
        /// constructor of such a collection.
        /// </remarks>
        public static bool IsWellKnownOrdinalComparer(IEqualityComparer<string?>? comparer, out bool ignoreCase)
        {
            if (comparer is IInternalStringEqualityComparer internalStringComparer)
            {
                comparer = internalStringComparer.GetUnderlyingEqualityComparer(); // unwrap if necessary
            }

            switch (comparer)
            {
                case StringComparer stringComparer:
                    return stringComparer.IsWellKnownOrdinalComparerCore(out ignoreCase);

                case StringEqualityComparer: // EqualityComparer<string>.Default
                case GenericEqualityComparer<string>: // EqualityComparer<string>.Default serialized
                    ignoreCase = false;
                    return true;

                default:
                    // unknown comparer
                    ignoreCase = default;
                    return false;
            }
        }

        private protected virtual bool IsWellKnownOrdinalComparerCore(out bool ignoreCase)
        {
            // unless specialized comparer overrides this, we're not a well-known ordinal comparer
            ignoreCase = default;
            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="IEqualityComparer{String}"/> is a well-known culture-aware string comparer.
        /// </summary>
        /// <param name="comparer">The comparer to query.</param>
        /// <param name="compareInfo">When this method returns, contains a value indicating which <see cref="CompareInfo"/> was used
        /// to create <paramref name="comparer"/>. Set to <see langword="null"/> if this method returns <see langword="false"/>.</param>
        /// <param name="compareOptions">When this method returns, contains a value indicating which <see cref="CompareOptions"/> was used
        /// to create <paramref name="comparer"/>. Set to <see cref="CompareOptions.None"/> if this method returns <see langword="false"/>.</param>
        /// whether <paramref name="comparer"/>
        /// <returns>
        /// <see langword="true"/> if <paramref name="comparer"/> is a well-known culture-aware string comparer;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// A "well-known culture-aware comparer" describes a comparer which is tied to a specific <see cref="CompareInfo"/> using
        /// some defined <see cref="CompareOptions"/>. To create a <see cref="StringComparer"/> instance wrapped around a
        /// <see cref="CompareInfo"/> and <see cref="CompareOptions"/>, use <see cref="GlobalizationExtensions.GetStringComparer(CompareInfo, CompareOptions)"/>.
        /// This method returns <see langword="false"/> when given <see cref="Ordinal"/> and other non-linguistic comparers as input.
        /// </remarks>
        public static bool IsWellKnownCultureAwareComparer(IEqualityComparer<string?>? comparer, [NotNullWhen(true)] out CompareInfo? compareInfo, out CompareOptions compareOptions)
        {
            if (comparer is IInternalStringEqualityComparer internalStringComparer)
            {
                comparer = internalStringComparer.GetUnderlyingEqualityComparer(); // unwrap if necessary
            }

            if (comparer is StringComparer stringComparer)
            {
                return stringComparer.IsWellKnownCultureAwareComparerCore(out compareInfo, out compareOptions);
            }
            else
            {
                // unknown comparer
                compareInfo = default;
                compareOptions = default;
                return false;
            }
        }

        private protected virtual bool IsWellKnownCultureAwareComparerCore([NotNullWhen(true)] out CompareInfo? compareInfo, out CompareOptions compareOptions)
        {
            // unless specialized comparer overrides this, we're not a well-known culture-aware comparer
            compareInfo = default;
            compareOptions = default;
            return false;
        }

        public int Compare(object? x, object? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            if (x is string sa)
            {
                if (y is string sb)
                {
                    return Compare(sa, sb);
                }
            }

            if (x is IComparable ia)
            {
                return ia.CompareTo(y);
            }

            throw new ArgumentException(SR.Argument_ImplementIComparable);
        }

        public new bool Equals(object? x, object? y)
        {
            if (x == y) return true;
            if (x == null || y == null) return false;

            if (x is string sa)
            {
                if (y is string sb)
                {
                    return Equals(sa, sb);
                }
            }
            return x.Equals(y);
        }

        public int GetHashCode(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            if (obj is string s)
            {
                return GetHashCode(s);
            }
            return obj.GetHashCode();
        }

        public abstract int Compare(string? x, string? y);
        public abstract bool Equals(string? x, string? y);
#pragma warning disable CS8614 // Remove warning disable when nullable attributes are respected
        public abstract int GetHashCode(string obj);
#pragma warning restore CS8614
    }

    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class CultureAwareComparer : StringComparer, IAlternateEqualityComparer<ReadOnlySpan<char>, string?>, ISerializable
    {
        internal static readonly CultureAwareComparer InvariantCaseSensitiveInstance = new CultureAwareComparer(CompareInfo.Invariant, CompareOptions.None);
        internal static readonly CultureAwareComparer InvariantIgnoreCaseInstance = new CultureAwareComparer(CompareInfo.Invariant, CompareOptions.IgnoreCase);

        private const CompareOptions ValidCompareMaskOffFlags =
            ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreKanaType |
              CompareOptions.IgnoreWidth | CompareOptions.NumericOrdering | CompareOptions.StringSort);

        private readonly CompareInfo _compareInfo; // Do not rename (binary serialization)
        private readonly CompareOptions _options;

        internal CultureAwareComparer(CultureInfo culture, CompareOptions options) : this(culture.CompareInfo, options) { }

        internal CultureAwareComparer(CompareInfo compareInfo, CompareOptions options)
        {
            _compareInfo = compareInfo;

            if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }
            _options = options;
        }

        private CultureAwareComparer(SerializationInfo info, StreamingContext context)
        {
            _compareInfo = (CompareInfo)info.GetValue("_compareInfo", typeof(CompareInfo))!;
            bool ignoreCase = info.GetBoolean("_ignoreCase");

            object? obj = info.GetValueNoThrow("_options", typeof(CompareOptions));
            if (obj != null)
                _options = (CompareOptions)obj;

            // fix up the _options value in case we are getting old serialized object not having _options
            _options |= ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
        }

        public override int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return _compareInfo.Compare(x, y, _options);
        }

        public override bool Equals(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            return _compareInfo.Compare(x, y, _options) == 0;
        }

        public override int GetHashCode(string obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            return _compareInfo.GetHashCode(obj, _options);
        }

        int IAlternateEqualityComparer<ReadOnlySpan<char>, string?>.GetHashCode(ReadOnlySpan<char> span) =>
            _compareInfo.GetHashCode(span, _options);

        bool IAlternateEqualityComparer<ReadOnlySpan<char>, string?>.Equals(ReadOnlySpan<char> span, string? target)
        {
            // See explanation in OrdinalComparer.Equals.
            if (span.IsEmpty && target is null)
            {
                return false;
            }

            return _compareInfo.Compare(span, target, _options) == 0;
        }

        string IAlternateEqualityComparer<ReadOnlySpan<char>, string?>.Create(ReadOnlySpan<char> span) =>
            span.ToString();

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return
                obj is CultureAwareComparer comparer &&
                _options == comparer._options &&
                _compareInfo.Equals(comparer._compareInfo);
        }

        public override int GetHashCode()
        {
            return _compareInfo.GetHashCode() ^ ((int)_options & 0x7FFFFFFF);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("_compareInfo", _compareInfo);
            info.AddValue("_options", _options);
            info.AddValue("_ignoreCase", (_options & CompareOptions.IgnoreCase) != 0);
        }

        private protected override bool IsWellKnownCultureAwareComparerCore([NotNullWhen(true)] out CompareInfo? compareInfo, out CompareOptions compareOptions)
        {
            compareInfo = _compareInfo;
            compareOptions = _options;
            return true;
        }
    }

    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class OrdinalComparer : StringComparer, IAlternateEqualityComparer<ReadOnlySpan<char>, string?>
    {
        private readonly bool _ignoreCase; // Do not rename (binary serialization)

        internal OrdinalComparer(bool ignoreCase)
        {
            _ignoreCase = ignoreCase;
        }

        public override int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;

            if (_ignoreCase)
            {
                return Globalization.Ordinal.CompareStringIgnoreCase(ref x.GetRawStringData(), x.Length, ref y.GetRawStringData(), y.Length);
            }

            return string.CompareOrdinal(x, y);
        }

        public override bool Equals(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x == null || y == null)
                return false;

            if (_ignoreCase)
            {
                if (x.Length != y.Length)
                {
                    return false;
                }
                return Globalization.Ordinal.EqualsIgnoreCase(ref x.GetRawStringData(), ref y.GetRawStringData(), x.Length);
            }
            return x.Equals(y);
        }

        public override int GetHashCode(string obj)
        {
            if (obj == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.obj);
            }

            if (_ignoreCase)
            {
                return obj.GetHashCodeOrdinalIgnoreCase();
            }

            return obj.GetHashCode();
        }

        bool IAlternateEqualityComparer<ReadOnlySpan<char>, string?>.Equals(ReadOnlySpan<char> span, string? target)
        {
            // Dictionary<string, TValue> does not permit null keys, but it does permit string.Empty keys,
            // so we'd like for ReadOnlySpan<char>.Empty (which contains a null ref) to map to string.Empty.
            // However, HashSet<string> permits both string.Empty and null string keys, and ReadOnlySpan<char>.Empty
            // shouldn't map to both, as that would break HashSet<string> lookups (e.g. the HashSet<string> could contain
            // both null and empty strings, which are not equal to each other, yet removing a
            // ReadOnlySpan<char>.Empty would only remove one of them?). That means we need to pick whether
            // ReadOnlySpan<char>.Empty is equivalent to string empty or null, and because of dictionary, we
            // should pick empty. That means if the span is empty (whether or not is has a null reference),
            // this should return false if the target string is null.
            if (span.IsEmpty && target is null)
            {
                return false;
            }

            return _ignoreCase?
                span.EqualsOrdinalIgnoreCase(target) :
                span.SequenceEqual(target);
        }

        int IAlternateEqualityComparer<ReadOnlySpan<char>, string?>.GetHashCode(ReadOnlySpan<char> span) =>
            _ignoreCase ?
                string.GetHashCodeOrdinalIgnoreCase(span) :
                string.GetHashCode(span);

        string IAlternateEqualityComparer<ReadOnlySpan<char>, string?>.Create(ReadOnlySpan<char> span) =>
            span.ToString();

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not OrdinalComparer comparer)
            {
                return false;
            }
            return this._ignoreCase == comparer._ignoreCase;
        }

        public override int GetHashCode()
        {
            int hashCode = nameof(OrdinalComparer).GetHashCode();
            return _ignoreCase ? (~hashCode) : hashCode;
        }

        private protected override bool IsWellKnownOrdinalComparerCore(out bool ignoreCase)
        {
            ignoreCase = _ignoreCase;
            return true;
        }
    }

    [Serializable]
    internal sealed class OrdinalCaseSensitiveComparer : OrdinalComparer, ISerializable, IAlternateEqualityComparer<ReadOnlySpan<char>, string?>
    {
        internal static readonly OrdinalCaseSensitiveComparer Instance = new OrdinalCaseSensitiveComparer();

        private OrdinalCaseSensitiveComparer() : base(false)
        {
        }

        public override int Compare(string? x, string? y) => string.CompareOrdinal(x, y);

        public override bool Equals(string? x, string? y) => string.Equals(x, y);

        public override int GetHashCode(string obj)
        {
            if (obj == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.obj);
            }
            return obj.GetHashCode();
        }

        bool IAlternateEqualityComparer<ReadOnlySpan<char>, string?>.Equals(ReadOnlySpan<char> span, string? target)
        {
            // See explanation in OrdinalComparer.Equals.
            if (span.IsEmpty && target is null)
            {
                return false;
            }

            return span.SequenceEqual(target);
        }

        int IAlternateEqualityComparer<ReadOnlySpan<char>, string?>.GetHashCode(ReadOnlySpan<char> span) =>
            string.GetHashCode(span);

        string IAlternateEqualityComparer<ReadOnlySpan<char>, string?>.Create(ReadOnlySpan<char> span) =>
            span.ToString();

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(OrdinalComparer));
            info.AddValue("_ignoreCase", false);
        }
    }

    [Serializable]
    internal sealed class OrdinalIgnoreCaseComparer : OrdinalComparer, IAlternateEqualityComparer<ReadOnlySpan<char>, string?>, ISerializable
    {
        internal static readonly OrdinalIgnoreCaseComparer Instance = new OrdinalIgnoreCaseComparer();

        private OrdinalIgnoreCaseComparer() : base(true)
        {
        }

        public override int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            return Globalization.Ordinal.CompareStringIgnoreCase(ref x.GetRawStringData(), x.Length, ref y.GetRawStringData(), y.Length);
        }

        public override bool Equals(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (x.Length != y.Length)
            {
                return false;
            }

            return Globalization.Ordinal.EqualsIgnoreCase(ref x.GetRawStringData(), ref y.GetRawStringData(), x.Length);
        }

        public override int GetHashCode(string obj)
        {
            if (obj == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.obj);
            }
            return obj.GetHashCodeOrdinalIgnoreCase();
        }

        bool IAlternateEqualityComparer<ReadOnlySpan<char>, string?>.Equals(ReadOnlySpan<char> span, string? target)
        {
            // See explanation in OrdinalComparer.Equals.
            if (span.IsEmpty && target is null)
            {
                return false;
            }

            return span.EqualsOrdinalIgnoreCase(target);
        }

        int IAlternateEqualityComparer<ReadOnlySpan<char>, string?>.GetHashCode(ReadOnlySpan<char> span) =>
            string.GetHashCodeOrdinalIgnoreCase(span);

        string IAlternateEqualityComparer<ReadOnlySpan<char>, string?>.Create(ReadOnlySpan<char> span) =>
            span.ToString();

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(OrdinalComparer));
            info.AddValue("_ignoreCase", true);
        }
    }
}
