using Piranha.Extend;

namespace CMS
{
    [FieldType(Name = "Multi-select field", Component = "multi-select-field")]
    public class MultiSelectField<T> : IField
    {
        public string GetTitle()
        {
            return string.Empty;
        }
        
        public IEnumerable<T> Value { get; set; }
    }
}