namespace WebApi
{
    public class QualityResultTable
    {
        public string Title { get; set; } = string.Empty;

        public List<string> Headers { get; } = new List<string>();

        public List<QualityResultTableRow> Rows { get; } = new List<QualityResultTableRow>();
    }
}
