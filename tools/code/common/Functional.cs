using LanguageExt;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace common;

public static class OptionExtensions
{
    public static T IfNoneThrow<T>(this Option<T> option, string errorMessage)
    {
        return option.IfNone(() => throw new InvalidOperationException(errorMessage));
    }

    public static T? IfNoneNull<T>(this Option<T> option) where T : class =>
        option.MatchUnsafe<T?>(t => t, () => null);

    // We use the object? _ = null to distinguish struct function from class function, otherwise the functions would need different names.
    public static T? IfNoneNull<T>(this Option<T> option, object? _ = null) where T : struct =>
        option.MatchUnsafe<T?>(t => t, () => null);
}

internal static class EitherExtensions
{
    public static T? IfLeftNull<T>(this Either<string, T> either) where T : class
    {
        return either.MatchUnsafe<T?>(t => t, error => null);
    }

    // We use the object? _ = null to distinguish struct function from class function, otherwise the functions would need different names.
    public static T? IfLeftNull<T>(this Either<string, T> either, object? _ = null) where T : struct
    {
        return either.MatchUnsafe<T?>(t => t, error => null);
    }
}

public static class IEnumerableExtensions
{
    ///// <summary>
    ///// Applies <paramref name="f"/> to <paramref name="enumerable"/> items and filters out
    ///// null results.
    ///// </summary>
    //public static IEnumerable<T2> Choose<T, T2>(this IEnumerable<T> enumerable, Func<T, T2?> f) where T2 : class
    //{
    //    return from t in enumerable
    //           let t2 = f(t)
    //           where t2 is not null
    //           select t2;
    //}

    ///// <summary>
    ///// Applies <paramref name="f"/> to <paramref name="enumerable"/> items and filters out
    ///// null results.
    ///// </summary>
    //public static IEnumerable<T2> Choose<T, T2>(this IEnumerable<T> enumerable, Func<T, T2?> f) where T2 : struct
    //{
    //    return from t in enumerable
    //           let t2 = f(t)
    //           where t2 is not null
    //           select t2.Value;
    //}

    //public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    //{
    //    foreach (var t in enumerable)
    //    {
    //        action(t);
    //    }
    //}

    public static async ValueTask ForEachParallel<T>(this IEnumerable<T> enumerable, Func<T, ValueTask> action, CancellationToken cancellationToken) =>
        await Parallel.ForEachAsync(enumerable,
                                    cancellationToken,
                                    (item, token) => action(item));

    //public static async ValueTask ForEachAwaitAsync<T>(this IEnumerable<T> enumerable, Func<T, ValueTask> action)
    //{
    //    foreach (var t in enumerable)
    //    {
    //        await action(t);
    //    }
    //}

    ///// <summary>
    ///// Returns an empty enumerable if <paramref name="enumerable"/> is null.
    ///// </summary>
    //public static IEnumerable<T> IfNullEmpty<T>(this IEnumerable<T>? enumerable) =>
    //    enumerable is null ? Enumerable.Empty<T>() : enumerable;

    //public static IEnumerable<T> FullJoin<T, TKey>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, TKey> keySelector, Func<T, T, T> bothSelector) =>
    //    first.FullJoin(second, keySelector, t => t, t => t, bothSelector);

    //public static IEnumerable<T> LeftJoin<T, TKey>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, TKey> keySelector, Func<T, T, T> bothSelector) =>
    //    first.LeftJoin(second, keySelector, t => t, bothSelector);
}

public static class IAsyncEnumerableExtensions
{
    public static async ValueTask ForEachParallel<T>(this IAsyncEnumerable<T> enumerable, Func<T, ValueTask> action, CancellationToken cancellationToken) =>
        await Parallel.ForEachAsync(enumerable,
                                    cancellationToken,
                                    (item, token) => action(item));
}