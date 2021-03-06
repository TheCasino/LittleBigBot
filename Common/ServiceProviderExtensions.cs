﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace LittleBigBot.Common
{
    public static class ServiceProviderExtensions
    {
        public static bool TryGetService(this IServiceProvider provider, Type type, out object result)
        {
            var query = provider.GetService(type);
            if (query != null)
            {
                result = query;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryGetService<T>(this IServiceProvider provider, out T result)
        {
            var query = provider.GetService<T>();
            if (query != null)
            {
                result = query;
                return true;
            }

            result = default;
            return false;
        }

        public static bool TryGetServices(this IServiceProvider provider, Type type, out IEnumerable<object> result)
        {
            var query = provider.GetServices(type);
            if (query == null)
            {
                result = null;
                return false;
            }

            var enumerable = query as object[] ?? query.ToArray();
            if (!enumerable.Any())
            {
                result = null;
                return false;
            }

            result = enumerable;
            return true;
        }

        public static bool TryGetServices<T>(this IServiceProvider provider, out IEnumerable<T> result)
        {
            var query = provider.GetServices<T>();
            if (query == null)
            {
                result = null;
                return false;
            }

            var enumerable = query as T[] ?? query.ToArray();
            if (!enumerable.Any())
            {
                result = null;
                return false;
            }

            result = enumerable;
            return true;
        }
    }
}