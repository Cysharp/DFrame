using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace DFrame.ComponentLibrary.Shared
{
    public sealed class InputSelectEnum<TEnum> : InputBase<TEnum>
    {
        // Generate Html
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "select");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "class", CssClass);
            builder.AddAttribute(3, "value", BindConverter.FormatValue(CurrentValueAsString));
            builder.AddAttribute(4, "onchange", EventCallback.Factory.CreateBinder<string>(this, value => CurrentValueAsString = value, CurrentValueAsString, null));

            // add an option element per enum value
            var i = 5;
            var enumType = GetEnumType();
            foreach (TEnum value in Enum.GetValues(enumType))
            {
                builder.OpenElement(i++, "option");
                builder.AddAttribute(i++, "value", value.ToString());
                builder.AddContent(i++, GetDisplayName(value));
                builder.CloseElement();
            }

            builder.CloseElement();
        }

        protected override bool TryParseValueFromString(string value, out TEnum result, out string validationErrorMessage)
        {
            // convert for Blazor component
            if (BindConverter.TryConvertTo(value, CultureInfo.CurrentCulture, out TEnum parsedValue))
            {
                result = parsedValue;
                validationErrorMessage = null;
                return true;
            }

            // nullable mapping
            if (string.IsNullOrEmpty(value))
            {
                var nullableType = Nullable.GetUnderlyingType(typeof(TEnum));
                if (nullableType != null)
                {
                    result = default;
                    validationErrorMessage = null;
                    return true;
                }
            }

            // error
            result = default;
            validationErrorMessage = $"The {FieldIdentifier.FieldName} field is not valid.";
            return false;
        }

        /// <summary>
        /// Get Name from <see cref="DisplayAttribute"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string GetDisplayName(TEnum value)
        {
            var member = value.GetType().GetMember(value.ToString())[0];
            var displayAttribute = member.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute != null)
                return displayAttribute.GetName();

            return value.ToString();
        }

        /// <summary>
        /// Get Enum Type, unwrap Nullable<TEnum>
        /// </summary>
        /// <returns></returns>
        private Type GetEnumType()
        {
            var nullableType = Nullable.GetUnderlyingType(typeof(TEnum));
            if (nullableType != null)
                return nullableType;

            return typeof(TEnum);
        }
    }
}
