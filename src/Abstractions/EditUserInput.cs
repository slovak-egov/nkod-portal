namespace NkodSk.Abstractions
{
    public class EditUserInput
    {
        public string? Id { get; set; }

        public string? Email { get; set; } 

        public string? Role { get; set; }

        public string FirstName { get;set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public ValidationResults Validate()
        {
            ValidationResults results = new ValidationResults();

            results.ValidateRequiredText(nameof(FirstName), FirstName);
            results.ValidateRequiredText(nameof(LastName), LastName);
            results.ValidateRequiredText(nameof(Email), Email);

            return results;
        }
    }
}
