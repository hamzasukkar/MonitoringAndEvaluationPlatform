using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace MonitoringAndEvaluationPlatform.Attributes
{
    public class DateRangeValidationAttribute : ValidationAttribute
    {
        private readonly string _startDatePropertyName;

        public DateRangeValidationAttribute(string startDatePropertyName)
        {
            _startDatePropertyName = startDatePropertyName;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var endDate = (DateTime)value;

            var startDateProperty = validationContext.ObjectType.GetProperty(_startDatePropertyName);
            if (startDateProperty == null)
                return new ValidationResult($"Unknown property: {_startDatePropertyName}");

            var startDateValue = startDateProperty.GetValue(validationContext.ObjectInstance);
            if (startDateValue == null)
                return ValidationResult.Success;

            var startDate = (DateTime)startDateValue;

            if (endDate <= startDate)
            {
                return new ValidationResult("End date must be after start date");
            }

            return ValidationResult.Success;
        }
    }
}