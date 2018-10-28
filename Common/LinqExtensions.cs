using System;
using System.Collections.Generic;
using System.Linq;
using Qmmands;

namespace LittleBigBot.Common
{
    public static class LinqExtensions
    {
        // I like my toys this way
        public static string Join<T>(this IEnumerable<T> source, string splitter)
        {
            return string.Join(splitter, source);
        }

        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> enumerable,
            int chunkSize)
        {
            if (chunkSize < 1) throw new ArgumentException("chunkSize must be positive");

            using (var e = enumerable.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    var remaining = chunkSize; // elements remaining in the current chunk
                    var innerMoveNext = new Func<bool>(() => --remaining > 0 && e.MoveNext());

                    yield return e.GetChunk(innerMoveNext);
                    while (innerMoveNext())
                    {
                        /* discard elements skipped by inner iterator */
                    }
                }
            }
        }

        private static IEnumerable<T> GetChunk<T>(this IEnumerator<T> e,
            Func<bool> innerMoveNext)
        {
            do
            {
                yield return e.Current;
            } while (innerMoveNext());
        }

        public static Module Search(this IEnumerable<Module> modules, string query,
            StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            return modules.FirstOrDefault(a =>
                a.Name.Equals(query, stringComparison) || a.Aliases.Any(ab => ab.Equals(query, stringComparison)));
        }
    }
}