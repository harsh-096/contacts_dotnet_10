using System.ComponentModel.DataAnnotations;

namespace ContactSystem.DTOs
{
    /// <summary>
    /// Class-level validator: at least one of the named properties must be non-null
    /// (and, for strings, non-empty). Used to forbid empty PATCH/PUT bodies.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class AtLeastOneAttribute : ValidationAttribute
    {
        private readonly string[] _propertyNames;

        public AtLeastOneAttribute(params string[] propertyNames)
        {
            _propertyNames = propertyNames ?? Array.Empty<string>();
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            if (value is null)
                return ValidationResult.Success;

            var type = value.GetType();
            foreach (var name in _propertyNames)
            {
                var prop = type.GetProperty(name);
                if (prop is null) continue;

                var val = prop.GetValue(value);
                if (val is null) continue;

                if (val is string s && string.IsNullOrWhiteSpace(s)) continue;

                return ValidationResult.Success;
            }

            var names = _propertyNames.Length == 0 ? "(no fields configured)" : string.Join(", ", _propertyNames);
            return new ValidationResult(
                ErrorMessage ?? $"At least one of the following fields must be provided: {names}.",
                _propertyNames);
        }
    }
}
