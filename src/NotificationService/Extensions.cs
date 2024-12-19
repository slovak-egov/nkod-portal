namespace NotificationService
{
    public static class Extensions
    {
        public static Dictionary<string, List<T>> GroupByKey<T>(this List<T> items, Func<T, string> keySelector)
        {
            Dictionary<string, List<T>> grouped = [];
            foreach (T item in items)
            {
                string key = keySelector(item);
                if (!grouped.TryGetValue(key, out List<T>? groupItems))
                {
                    groupItems = [];
                    grouped[key] = groupItems;
                }
                groupItems.Add(item);
            }
            return grouped;
        }
    }
}
