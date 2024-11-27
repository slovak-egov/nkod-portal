using System.ComponentModel.DataAnnotations;

namespace CMS.Suggestions
{
    public class RequiredIfCustomAttribute : ValidationAttribute
    {
        private readonly string otherProperty;
        private readonly object[] targetValues;

        public RequiredIfCustomAttribute(string otherProperty, params object[] targetValues)
        {
            this.otherProperty = otherProperty;
            this.targetValues = targetValues;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var otherPropertyValue = validationContext.ObjectType
                .GetProperty(otherProperty)?
                .GetValue(validationContext.ObjectInstance);
            if (!targetValues.Any(t => t.Equals(otherPropertyValue))) return ValidationResult.Success;
            if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult(ErrorMessage ?? "This field is required.");
            }
            
            return ValidationResult.Success;
        }
    }
}