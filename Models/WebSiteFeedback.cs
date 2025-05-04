using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class WebSiteFeedback
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [MaxLength(500)]
        public string Comment { get; set; }

        public DateTime SubmittedOn { get; set; } = DateTime.UtcNow;
    }
}
