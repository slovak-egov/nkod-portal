using Piranha.Extend;

namespace CMS.Applications
{
    [FieldType(Name = "Multi-select field", Component = "multi-select-field")]
    public class MultiSelectField : IField
    {
        public string GetTitle()
        {
            return string.Empty;
        }
        
        public IEnumerable<string> Value { get; set; }
    }
}