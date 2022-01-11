namespace DFrame
{
    public struct ConditionalValue<TValue>
    {
        public bool HasValue { get; }
        public TValue? Value { get; }

        public ConditionalValue(bool hasValue, TValue? value)
        {
            this.HasValue = hasValue;
            this.Value = value;
        }
    }

    internal static class ConditionalValueExtensions
    {
        public static ConditionalValue<T> As<T>(this ConditionalValue<object> value)
        {
            if (value.HasValue)
            {
                return new ConditionalValue<T>(true, (T?)value.Value);
            }
            else
            {
                return new ConditionalValue<T>(false, default!);
            }
        }
    }
}