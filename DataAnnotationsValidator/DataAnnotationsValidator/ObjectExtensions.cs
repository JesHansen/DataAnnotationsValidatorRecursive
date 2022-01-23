﻿namespace CorePort;

/// <summary>
/// Extracts values of properties via reflection.
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    /// Gets the value of a property.
    /// </summary>
    public static object? GetPropertyValue(this object o, string propertyName)
    {
        object? objValue = string.Empty;

        var propertyInfo = o.GetType().GetProperty(propertyName);
        if (propertyInfo != null)
            objValue = propertyInfo.GetValue(o, null);

        return objValue;
    }
}