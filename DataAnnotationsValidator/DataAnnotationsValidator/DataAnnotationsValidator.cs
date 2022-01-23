using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CorePort;

/// <inheritdoc />
public class DataAnnotationsValidator : IDataAnnotationsValidator
{
    /// <inheritdoc />
    public bool TryValidateObject(object obj, ICollection<ValidationResult> results, IDictionary<object, object?>? validationContextItems = null)
    {
        return Validator.TryValidateObject(obj, new ValidationContext(obj, null, validationContextItems), results, true);
    }

    /// <inheritdoc />
    public bool TryValidateObjectRecursive<T>(T obj, List<ValidationResult> results, IDictionary<object, object?>? validationContextItems = null)
    {
        return TryValidateObjectRecursive(obj, results, new HashSet<object>(), validationContextItems);
    }

    private bool TryValidateObjectRecursive<T>(T obj, List<ValidationResult> results, ISet<object> validatedObjects, IDictionary<object, object?>? validationContextItems = null)
    {
        //short-circuit to avoid infinite loops on cyclical object graphs
        if (obj is null || validatedObjects.Contains(obj))
        {
            return true;
        }

        validatedObjects.Add(obj);
        bool result = TryValidateObject(obj, results, validationContextItems);

        var properties = obj.GetType().GetProperties().Where(prop => 
            prop.CanRead && 
            !prop.GetCustomAttributes(typeof(SkipRecursiveValidation), false).Any() && 
            prop.GetIndexParameters().Length == 0)
            .ToList();

        foreach (var property in properties)
        {
            if (property.PropertyType == typeof(string) || property.PropertyType.IsValueType)
            {
                continue;
            }
            
            var value = obj.GetPropertyValue(property.Name);

            switch (value)
            {
                case null:
                    continue;
                case IEnumerable asEnumerable:
                {
                    result = HandleEnumerables<T>(results, validatedObjects, validationContextItems, asEnumerable, result, property);
                    break;
                }
                default:
                {
                    result = HandleDefault<T>(results, validatedObjects, validationContextItems, value, result, property);
                    break;
                }
            }
        }

        return result;
    }

    private bool HandleDefault<T>(List<ValidationResult> results, ISet<object> validatedObjects, IDictionary<object, object?>? validationContextItems, object value,
        bool result, PropertyInfo property)
    {
        var nestedResults = new List<ValidationResult>();
        if (TryValidateObjectRecursive(value, nestedResults, validatedObjects, validationContextItems))
        {
            return result;
        }

        results.AddRange(
            from validationResult in nestedResults 
            let property1 = property 
            select new ValidationResult(validationResult.ErrorMessage, validationResult.MemberNames.Select(x => property1.Name + '.' + x)));

        return false;
    }

    private bool HandleEnumerables<T>(List<ValidationResult> results, ISet<object> validatedObjects, IDictionary<object, object?>? validationContextItems,
        IEnumerable asEnumerable, bool result, PropertyInfo property)
    {
        foreach (var enumObj in asEnumerable)
        {
            if (enumObj == null)
            {
                continue;
            }

            var nestedResults = new List<ValidationResult>();
            if (TryValidateObjectRecursive(enumObj, nestedResults, validatedObjects, validationContextItems))
            {
                continue;
            }
            result = false;
            results.AddRange(
                from validationResult in nestedResults
                let property1 = property
                select new ValidationResult(validationResult.ErrorMessage,
                    validationResult.MemberNames.Select(x => property1.Name + '.' + x)));
        }

        return result;
    }
}