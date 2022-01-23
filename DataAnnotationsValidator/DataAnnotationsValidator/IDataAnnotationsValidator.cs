using System.ComponentModel.DataAnnotations;

namespace CorePort;

/// <summary>
/// I recursive data annotation validator
/// </summary>
public interface IDataAnnotationsValidator
{
    /// <summary>
    /// Shallow validation. Validate this object
    /// </summary>
    bool TryValidateObject(object obj, ICollection<ValidationResult> results, IDictionary<object, object?>? validationContextItems = null);
    /// <summary>
    /// Recursive validation of the object graph.
    /// </summary>
    bool TryValidateObjectRecursive<T>(T obj, List<ValidationResult> results, IDictionary<object, object?>? validationContextItems = null);
}