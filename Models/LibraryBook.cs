namespace WebApplication2.Models
{
    public class LibraryBook
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Publisher { get; set; }
        public int YearPublished { get; set; }
        public string CoverImage { get; set; }
        public bool IsBorrowed { get; set; }
        public string RemainingTime { get; set; }
    }
}
