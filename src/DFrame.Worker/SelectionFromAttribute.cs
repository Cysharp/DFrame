using System;

namespace DFrame;

[AttributeUsage(AttributeTargets.Parameter)]
public class SelectionFromAttribute : Attribute
{
    /// <summary>
    /// Name of Selector method
    /// </summary>
    /// <remarks>
    /// Example for:
    /// <![CDATA[
    /// private static IEnumerable<(string label, int value)> GetSomeParameterSelection() => ...;
    /// public SomeWorkload([SelectionFrom(nameof(GetSomeParameterSelection))] int someParameter) ...
    /// ]]>
    /// </remarks>
    public string SelectorMethodName { get; }

    public SelectionFromAttribute(string selectorMethodName) => SelectorMethodName = selectorMethodName;
}
