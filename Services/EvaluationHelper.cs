using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using LittleBigBot.Entities;

namespace LittleBigBot.Services
{
    public class EvaluationHelper
    {
        public EvaluationHelper(LittleBigBotExecutionContext context, IServiceProvider provider)
        {
            Services = provider;
            Context = context;
        }

        public IServiceProvider Services { get; }

        public LittleBigBotExecutionContext Context { get; }

        public Task<RestUserMessage> ReplyAsync(string content, Embed embed = null)
        {
            return Context.Channel.SendMessageAsync(content, embed: embed);
        }

        public string InspectMethods(object obj)
        {
            var type = obj as Type ?? obj.GetType();

            var sb = new StringBuilder();

            var methods = type.GetMethods();

            sb.AppendLine($"<< Inspecting methods for type [{type.Name}] >>");
            sb.AppendLine();

            foreach (var method in methods.Where(m => !m.IsSpecialName))
            {
                if (sb.Length > 1800) break;
                sb.AppendLine(
                    $"[Name: {method.Name}, Return-Type: {method.ReturnType.Name}, Parameters: [{string.Join(", ", method.GetParameters().Select(a => $"({a.ParameterType.Name} {a.Name})"))}]]");
                sb.AppendLine();
            }

            return Format.Code(sb.ToString(), "ini");
        }

        public string GetValue(object prop, object obj)
        {
            object value;

            /* PropertyInfo and FieldInfo both derive from MemberInfo, but that does not have a GetValue method, so the only
                supported ancestor is object */
            switch (prop)
            {
                case PropertyInfo pinfo:
                    value = pinfo.GetValue(obj);
                    break;
                case FieldInfo finfo:
                    value = finfo.GetValue(obj);
                    break;
                default:
                    throw new InvalidOperationException(
                        "GetValue(object, object): first parameter prop must be PropertyInfo or FieldInfo");
            }

            if (value != null)
            {
                string HandleEnumerable(IEnumerable enumerable)
                {
                    var enu = enumerable.Cast<object>().ToList();
                    return $"{enu.Count} [{enu.GetType().Name}]";
                }

                string HandleNormal()
                {
                    return value + $" [{value.GetType().Name}]";
                }

                switch (value)
                {
                    case IEnumerable enumerable:
                        if (value is string) return HandleNormal();
                        return HandleEnumerable(enumerable);
                    default:
                        return HandleNormal();
                }
            }

            return "Null";
        }

        public string InspectInheritance<T>()
        {
            return InspectInheritance(typeof(T));
        }

        public string InspectInheritance(Type type)
        {
            var parents = new List<List<Type>>();
            var latestType = type.BaseType;

            while (latestType != null)
            {
                var l = new List<Type> {latestType};
                l.AddRange(latestType.GetInterfaces());
                l.Reverse();
                parents.Add(l);
                latestType = latestType.BaseType;
            }

            if (parents.Count != 1)
            {
                var l = new List<Type> {type};
                l.AddRange(type.GetInterfaces());
                l.Reverse();
                parents.Insert(0, l);
            }

            parents.Reverse();

            string FormatType(Type atype)
            {
                var vs = atype.Namespace + "." + atype.Name;

                var t = atype.GenericTypeArguments;

                if (t.Any()) vs += $"<{string.Join(", ", t.Select(a => a.Name))}>";

                return vs;
            }

            var index = 1;
            return Format.Code(new StringBuilder()
                .AppendLine("Inheritance graph for type [" + type.FullName + "]")
                .AppendLine()
                .AppendLine(string.Join("\n\n",
                    parents.Select(ab =>
                    {
                        return index++ + ") " + string.Join(" -> ", ab.Select(b => $"[{FormatType(b)}]"));
                    })))
                .AppendLine()
                .AppendLine(string.Join(" -> ",
                    parents.Where(a => a.Any(b => !b.IsInterface))
                        .Select(d => "[" + d.FirstOrDefault(bx => !bx.IsInterface)?.Name + "]")))
                .ToString(), "ini");
        }

        public string InspectInheritance(object obj)
        {
            return InspectInheritance(obj.GetType());
        }

        public string Inspect(object obj)
        {
            var type = obj.GetType();

            var inspection = new StringBuilder();
            inspection.AppendLine($"<< Inspecting type [{type.Name}] >>");
            inspection.AppendLine($"<< String Representation: [{obj}] >>");
            inspection.AppendLine();

            /* Get list of properties, with no index parameters (to avoid exceptions) */
            var props = type.GetProperties().Where(a => a.GetIndexParameters().Length == 0)
                .OrderBy(a => a.Name).ToList();

            /* Get list of fields */
            var fields = type.GetFields().OrderBy(a => a.Name).ToList();

            /* Handle properties in type */
            if (props.Count != 0)
            {
                /* Add header if we have fields as well */
                if (fields.Count != 0) inspection.AppendLine("<< Properties >>");

                /* Get the longest named property in the list, so we can make the column width that + 5 */
                var columnWidth = props.Max(a => a.Name.Length) + 5;
                foreach (var prop in props)
                {
                    /* Crude skip to avoid request errors */
                    if (inspection.Length > 1800) break;

                    /* Create a blank string gap of the remaining space to the end of the column */
                    var sep = new string(' ', columnWidth - prop.Name.Length);

                    /* Add the property name, then the separator, then the value */
                    inspection.AppendLine($"{prop.Name}{sep}{(prop.CanRead ? GetValue(prop, obj) : "Unreadable")}");
                }
            }

            /* Repeat the same with fields */
            if (fields.Count != 0)
            {
                if (props.Count != 0)
                {
                    inspection.AppendLine();
                    inspection.AppendLine("<< Fields >>");
                }

                var columnWidth = fields.Max(ab => ab.Name.Length) + 5;
                foreach (var prop in fields)
                {
                    if (inspection.Length > 1800) break;

                    var sep = new string(' ', columnWidth - prop.Name.Length);
                    inspection.AppendLine($"{prop.Name}:{sep}{GetValue(prop, obj)}");
                }
            }

            /* If the object is an enumerable type, add a list of it's items */
            if (obj is IEnumerable objEnumerable)
            {
                inspection.AppendLine();
                inspection.AppendLine("<< Items >>");
                foreach (var prop in objEnumerable) inspection.AppendLine($" - {prop}");
            }

            return Format.Code(inspection.ToString(), "ini");
        }
    }
}