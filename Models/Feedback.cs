using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class Feedback
    {
        public int FeedbackId { get; set; }

        public int BookId { get; set; }
        public string UserId { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        public string Comment { get; set; }

        public DateTime FeedbackDate { get; set; }

        // Navigation properties
        public Book Book { get; set; }
        public User User { get; set; }

    }

}
