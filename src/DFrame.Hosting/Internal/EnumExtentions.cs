using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace DFrame.Hosting.Internal
{
    internal static class EnumExtentions
    {
        /// <summary>
        /// Get DisplayName from Enum
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetDisplayName<TEnum>(this TEnum value) where TEnum: Enum
        {
            var member = value.GetType().GetMember(value.ToString())[0];
            var displayAttribute = member.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute != null)
                return displayAttribute.GetName();

            return value.ToString();
        }
    }
}
