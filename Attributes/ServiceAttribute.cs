using System;
using Microsoft.Extensions.DependencyInjection;

namespace LittleBigBot.Attributes
{
    public class ServiceAttribute : Attribute
    {
        public ServiceAttribute(string name, string description = null, ServiceLifetime lifetime = ServiceLifetime.Singleton, bool autoAdd = true)
        {
            Name = name;
            Description = description ?? "None.";
            Lifetime = lifetime;
            AutoAdd = autoAdd;
        }

        public string Name { get; }
        public string Description { get; }
        public ServiceLifetime Lifetime { get; }
        public bool AutoAdd { get; }
    }
}