using System;
using System.Collections.Generic;
using System.Linq;
using Qmmands;

namespace LittleBigBot.Common
{
    public static class LinqExtensions
    {
        public static Module Search(this IEnumerable<Module> modules, string query,
            StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            return modules.FirstOrDefault(a =>
                a.Name.Equals(query, stringComparison) || a.Aliases.Any(ab => ab.Equals(query, stringComparison)));
        }
    }
}