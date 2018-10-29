using System;

namespace LittleBigBot.Attributes
{
    /// <summary>
    ///     Provides a resource string to the consumer detailing what the default value for a command parameter is.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class DefaultValueDescriptionAttribute : Attribute
    {
        /// <summary>
        ///     Provides a resource string to the consumer detailing what the default value for a command parameter is.
        /// </summary>
        public DefaultValueDescriptionAttribute(string defaultValueDescription)
        {
            DefaultValueDescription = defaultValueDescription;
        }

        public string DefaultValueDescription { get; }
    }
}