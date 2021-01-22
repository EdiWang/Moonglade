namespace Moonglade.Core
{
    public class Archive
    {
        public int Year { get; }
        public int Month { get; }
        public int Count { get; }

        public Archive(int year, int month, int count)
        {
            Year = year;
            Month = month;
            Count = count;
        }
    }
}
