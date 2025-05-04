namespace WebApplication2.Models
{
    public class WaitingList
    {
        public int WaitingListId { get; set; }
        public int BookId { get; set; }
        public string UserId { get; set; }
        public DateTime AddedDate { get; set; }
        public int Position { get; set; }

        // Navigation properties
        public Book Book { get; set; }
        public User User { get; set; }

    }

}
