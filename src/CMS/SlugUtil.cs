using System.Text;

namespace CMS
{
	public static class SlugUtil
	{
		public static string Slugify(string input)
		{
			if (string.IsNullOrEmpty(input))
			{
				throw new ArgumentNullException("input");
			}

			var stringBuilder = new StringBuilder();
			foreach (char c in input.ToArray())
			{
				if (Char.IsLetterOrDigit(c))
				{
					stringBuilder.Append(c);
				}
				else if (c == ' ')
				{
					stringBuilder.Append("-");
				}
			}

			return stringBuilder.ToString().ToLower();
		}
	}
}
