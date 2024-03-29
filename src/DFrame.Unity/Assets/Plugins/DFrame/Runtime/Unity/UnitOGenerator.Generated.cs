﻿// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY UnitGenerator. DO NOT CHANGE IT.
// </auto-generated>
#pragma warning disable CS8669
using System;

using MessagePack;
using MessagePack.Formatters;

namespace DFrame
{

    [MessagePackFormatter(typeof(ExecutionIdMessagePackFormatter))]
    [System.ComponentModel.TypeConverter(typeof(ExecutionIdTypeConverter))]
    public readonly partial struct ExecutionId : IEquatable<ExecutionId>, IComparable<ExecutionId>
    {
        readonly System.Guid value;

        public System.Guid AsPrimitive() => value;

        public ExecutionId(System.Guid value)
        {
            this.value = value;
        }


        public static explicit operator System.Guid(ExecutionId value)
        {
            return value.value;
        }

        public static explicit operator ExecutionId(System.Guid value)
        {
            return new ExecutionId(value);
        }

        public bool Equals(ExecutionId other)
        {
            return value.Equals(other.value);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            var t = obj.GetType();
            if (t == typeof(ExecutionId))
            {
                return Equals((ExecutionId)obj);
            }
            if (t == typeof(System.Guid))
            {
                return value.Equals((System.Guid)obj);
            }

            return value.Equals(obj);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public static bool operator ==(in ExecutionId x, in ExecutionId y)
        {
            return x.value.Equals(y.value);
        }

        public static bool operator !=(in ExecutionId x, in ExecutionId y)
        {
            return !x.value.Equals(y.value);
        }

        public static readonly ExecutionId Empty = default(ExecutionId);

        public static ExecutionId New()
        {
            return new ExecutionId(Guid.NewGuid());
        }

        public static ExecutionId NewExecutionId()
        {
            return new ExecutionId(Guid.NewGuid());
        }




        // UnitGenerateOptions.ParseMethod

        public static ExecutionId Parse(string s)
        {
            return new ExecutionId(System.Guid.Parse(s));
        }

        public static bool TryParse(string s, out ExecutionId result)
        {
            if (System.Guid.TryParse(s, out var r))
            {
                result = new ExecutionId(r);
                return true;
            }
            else
            {
                result = default(ExecutionId);
                return false;
            }
        }







        // UnitGenerateOptions.Comparable

        public int CompareTo(ExecutionId other)
        {
            return value.CompareTo(other.value);
        }





        // UnitGenerateOptions.MessagePackFormatter
        private class ExecutionIdMessagePackFormatter : IMessagePackFormatter<ExecutionId>
        {
            public void Serialize(ref MessagePackWriter writer, ExecutionId value, MessagePackSerializerOptions options)
            {
                options.Resolver.GetFormatterWithVerify<System.Guid>().Serialize(ref writer, value.value, options);
            }

            public ExecutionId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                return new ExecutionId(options.Resolver.GetFormatterWithVerify<System.Guid>().Deserialize(ref reader, options));
            }
        }



        // Default
        private class ExecutionIdTypeConverter : System.ComponentModel.TypeConverter
        {
            private static readonly Type WrapperType = typeof(ExecutionId);
            private static readonly Type ValueType = typeof(System.Guid);

            public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == WrapperType || sourceType == ValueType)
                {
                    return true;
                }

                return base.CanConvertFrom(context, sourceType);
            }

            public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == WrapperType || destinationType == ValueType)
                {
                    return true;
                }

                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                if (value != null)
                {
                    var t = value.GetType();
                    if (t == typeof(ExecutionId))
                    {
                        return (ExecutionId)value;
                    }
                    if (t == typeof(System.Guid))
                    {
                        return new ExecutionId((System.Guid)value);
                    }
                }

                return base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
            {
                if (value is ExecutionId wrappedValue)
                {
                    if (destinationType == WrapperType)
                    {
                        return wrappedValue;
                    }

                    if (destinationType == ValueType)
                    {
                        return wrappedValue.AsPrimitive();
                    }
                }

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}

namespace DFrame
{

    [MessagePackFormatter(typeof(WorkerIdMessagePackFormatter))]
    [System.ComponentModel.TypeConverter(typeof(WorkerIdTypeConverter))]
    public readonly partial struct WorkerId : IEquatable<WorkerId>, IComparable<WorkerId>
    {
        readonly System.Guid value;

        public System.Guid AsPrimitive() => value;

        public WorkerId(System.Guid value)
        {
            this.value = value;
        }


        public static explicit operator System.Guid(WorkerId value)
        {
            return value.value;
        }

        public static explicit operator WorkerId(System.Guid value)
        {
            return new WorkerId(value);
        }

        public bool Equals(WorkerId other)
        {
            return value.Equals(other.value);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            var t = obj.GetType();
            if (t == typeof(WorkerId))
            {
                return Equals((WorkerId)obj);
            }
            if (t == typeof(System.Guid))
            {
                return value.Equals((System.Guid)obj);
            }

            return value.Equals(obj);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public static bool operator ==(in WorkerId x, in WorkerId y)
        {
            return x.value.Equals(y.value);
        }

        public static bool operator !=(in WorkerId x, in WorkerId y)
        {
            return !x.value.Equals(y.value);
        }

        public static readonly WorkerId Empty = default(WorkerId);

        public static WorkerId New()
        {
            return new WorkerId(Guid.NewGuid());
        }

        public static WorkerId NewWorkerId()
        {
            return new WorkerId(Guid.NewGuid());
        }




        // UnitGenerateOptions.ParseMethod

        public static WorkerId Parse(string s)
        {
            return new WorkerId(System.Guid.Parse(s));
        }

        public static bool TryParse(string s, out WorkerId result)
        {
            if (System.Guid.TryParse(s, out var r))
            {
                result = new WorkerId(r);
                return true;
            }
            else
            {
                result = default(WorkerId);
                return false;
            }
        }







        // UnitGenerateOptions.Comparable

        public int CompareTo(WorkerId other)
        {
            return value.CompareTo(other.value);
        }





        // UnitGenerateOptions.MessagePackFormatter
        private class WorkerIdMessagePackFormatter : IMessagePackFormatter<WorkerId>
        {
            public void Serialize(ref MessagePackWriter writer, WorkerId value, MessagePackSerializerOptions options)
            {
                options.Resolver.GetFormatterWithVerify<System.Guid>().Serialize(ref writer, value.value, options);
            }

            public WorkerId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                return new WorkerId(options.Resolver.GetFormatterWithVerify<System.Guid>().Deserialize(ref reader, options));
            }
        }



        // Default
        private class WorkerIdTypeConverter : System.ComponentModel.TypeConverter
        {
            private static readonly Type WrapperType = typeof(WorkerId);
            private static readonly Type ValueType = typeof(System.Guid);

            public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == WrapperType || sourceType == ValueType)
                {
                    return true;
                }

                return base.CanConvertFrom(context, sourceType);
            }

            public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == WrapperType || destinationType == ValueType)
                {
                    return true;
                }

                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                if (value != null)
                {
                    var t = value.GetType();
                    if (t == typeof(WorkerId))
                    {
                        return (WorkerId)value;
                    }
                    if (t == typeof(System.Guid))
                    {
                        return new WorkerId((System.Guid)value);
                    }
                }

                return base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
            {
                if (value is WorkerId wrappedValue)
                {
                    if (destinationType == WrapperType)
                    {
                        return wrappedValue;
                    }

                    if (destinationType == ValueType)
                    {
                        return wrappedValue.AsPrimitive();
                    }
                }

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}


namespace DFrame
{

    [MessagePackFormatter(typeof(WorkloadIdMessagePackFormatter))]
    [System.ComponentModel.TypeConverter(typeof(WorkloadIdTypeConverter))]
    public readonly partial struct WorkloadId : IEquatable<WorkloadId>, IComparable<WorkloadId>
    {
        readonly System.Guid value;

        public System.Guid AsPrimitive() => value;

        public WorkloadId(System.Guid value)
        {
            this.value = value;
        }


        public static explicit operator System.Guid(WorkloadId value)
        {
            return value.value;
        }

        public static explicit operator WorkloadId(System.Guid value)
        {
            return new WorkloadId(value);
        }

        public bool Equals(WorkloadId other)
        {
            return value.Equals(other.value);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            var t = obj.GetType();
            if (t == typeof(WorkloadId))
            {
                return Equals((WorkloadId)obj);
            }
            if (t == typeof(System.Guid))
            {
                return value.Equals((System.Guid)obj);
            }

            return value.Equals(obj);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public static bool operator ==(in WorkloadId x, in WorkloadId y)
        {
            return x.value.Equals(y.value);
        }

        public static bool operator !=(in WorkloadId x, in WorkloadId y)
        {
            return !x.value.Equals(y.value);
        }

        public static readonly WorkloadId Empty = default(WorkloadId);

        public static WorkloadId New()
        {
            return new WorkloadId(Guid.NewGuid());
        }

        public static WorkloadId NewWorkloadId()
        {
            return new WorkloadId(Guid.NewGuid());
        }




        // UnitGenerateOptions.ParseMethod

        public static WorkloadId Parse(string s)
        {
            return new WorkloadId(System.Guid.Parse(s));
        }

        public static bool TryParse(string s, out WorkloadId result)
        {
            if (System.Guid.TryParse(s, out var r))
            {
                result = new WorkloadId(r);
                return true;
            }
            else
            {
                result = default(WorkloadId);
                return false;
            }
        }







        // UnitGenerateOptions.Comparable

        public int CompareTo(WorkloadId other)
        {
            return value.CompareTo(other.value);
        }





        // UnitGenerateOptions.MessagePackFormatter
        private class WorkloadIdMessagePackFormatter : IMessagePackFormatter<WorkloadId>
        {
            public void Serialize(ref MessagePackWriter writer, WorkloadId value, MessagePackSerializerOptions options)
            {
                options.Resolver.GetFormatterWithVerify<System.Guid>().Serialize(ref writer, value.value, options);
            }

            public WorkloadId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                return new WorkloadId(options.Resolver.GetFormatterWithVerify<System.Guid>().Deserialize(ref reader, options));
            }
        }



        // Default
        private class WorkloadIdTypeConverter : System.ComponentModel.TypeConverter
        {
            private static readonly Type WrapperType = typeof(WorkloadId);
            private static readonly Type ValueType = typeof(System.Guid);

            public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == WrapperType || sourceType == ValueType)
                {
                    return true;
                }

                return base.CanConvertFrom(context, sourceType);
            }

            public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == WrapperType || destinationType == ValueType)
                {
                    return true;
                }

                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                if (value != null)
                {
                    var t = value.GetType();
                    if (t == typeof(WorkloadId))
                    {
                        return (WorkloadId)value;
                    }
                    if (t == typeof(System.Guid))
                    {
                        return new WorkloadId((System.Guid)value);
                    }
                }

                return base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
            {
                if (value is WorkloadId wrappedValue)
                {
                    if (destinationType == WrapperType)
                    {
                        return wrappedValue;
                    }

                    if (destinationType == ValueType)
                    {
                        return wrappedValue.AsPrimitive();
                    }
                }

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}
