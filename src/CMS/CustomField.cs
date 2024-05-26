using Piranha.Extend;

namespace CMS
{
    [FieldType(Name = "Custom field", Component = "custom-field")]
    public class CustomField<T> : IField
    {
        public string GetTitle()
        {
            return string.Empty;
        }
        
        public T Value { get; set; }
    }
}