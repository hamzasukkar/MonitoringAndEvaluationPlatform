using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Attributes
{
    /// <summary>
    /// Requires an exchange rate whenever the project is denominated in anything other than
    /// the base currency (SYP), so a foreign-currency budget can always be converted for
    /// cross-project totals. SYP projects convert 1:1 and need no rate.
    /// </summary>
    public class RequiredWhenCurrencyNotSypAttribute : ValidationAttribute
    {
        private const string BaseCurrency = "SYP";

        private readonly string _currencyPropertyName;

        public RequiredWhenCurrencyNotSypAttribute(string currencyPropertyName)
        {
            _currencyPropertyName = currencyPropertyName;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var currencyProperty = validationContext.ObjectType.GetProperty(_currencyPropertyName);
            if (currencyProperty == null)
                return new ValidationResult($"Unknown property: {_currencyPropertyName}");

            var currency = currencyProperty.GetValue(validationContext.ObjectInstance) as string;

            // No currency chosen yet: that field's own validation reports it. Staying silent
            // here avoids showing two errors for one mistake.
            if (string.IsNullOrWhiteSpace(currency))
                return ValidationResult.Success;

            if (string.Equals(currency, BaseCurrency, StringComparison.OrdinalIgnoreCase))
                return ValidationResult.Success;

            if (value is decimal rate && rate > 0)
                return ValidationResult.Success;

            // Go through FormatErrorMessage so a configured resource wins. Reading ErrorMessage
            // directly would miss it: when ErrorMessageResourceType/Name are set the base class
            // leaves ErrorMessage null and exposes the localized text separately.
            var message = ErrorMessage is not null || ErrorMessageResourceName is not null
                ? FormatErrorMessage(validationContext.DisplayName ?? string.Empty)
                : "An exchange rate is required for this currency.";

            return new ValidationResult(
                message,
                new[] { validationContext.MemberName ?? string.Empty });
        }
    }
}
