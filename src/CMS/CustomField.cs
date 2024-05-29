using Piranha.Extend;

namespace CMS
{
    [FieldType(Name = "Custom field", Component = "custom-field")]
    public class CustomField<T> : IField, IComparable<CustomField<T>> where T : IComparable
    {
        public string GetTitle()
        {
            return string.Empty;
        }

		public int CompareTo(CustomField<T> other)
		{
            return this.Value.CompareTo(other.Value);
		}

		public T Value { get; set; }
    }
}