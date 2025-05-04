using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class Book
    {
        public int BookId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Author is required.")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Author can only contain letters.")]
        [MaxLength(100, ErrorMessage = "Author cannot exceed 100 characters.")]
        public string Author { get; set; }

        [Required(ErrorMessage = "Publisher is required.")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Publisher can only contain letters.")]
        [MaxLength(100, ErrorMessage = "Publisher cannot exceed 100 characters.")]
        public string Publisher { get; set; }

        [Range(1500, 2025, ErrorMessage = "Year must be between 1500 and 2025.")]
        public int YearPublished { get; set; }

        [Required(ErrorMessage = "Genre is required.")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Genre can only contain letters.")]
        [MaxLength(50, ErrorMessage = "Genre cannot exceed 50 characters.")]
        public string Genre { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price for buying must be greater than zero.")]
        public decimal PriceBuy { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price for borrowing must be greater than zero.")]
        public decimal PriceBorrow { get; set; }

        public bool IsBorrowable { get; set; }

        public bool IsDiscounted { get; set; }

        [Display(Name = "Discount End Date")]
        [DataType(DataType.Date)]
        public DateTime? DiscountEndDate { get; set; }


        [Range(0, double.MaxValue, ErrorMessage = "Discounted price must be a positive value.")]
        public decimal? DiscountedPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Copies available must be a positive value.")]
        public int CopiesAvailable { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Age limit must be a positive value.")]
        public int AgeLimit { get; set; }

        [Required(ErrorMessage = "Cover image URL is required.")]
        [RegularExpression(@"^/images\/[\w\-]+\.(png|jpg)$", ErrorMessage = "Cover image must be a valid URL in the format: /images/nameOfImage.png or /images/nameOfImage.jpg.")]
        public string CoverImage { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Popularity must be a positive value.")]
        public int Popularity { get; set; }

        [Required(ErrorMessage = "Available formats are required.")]
        [RegularExpression(@"^(epub|fb2|mobi|pdf)(,(epub|fb2|mobi|pdf))*$", ErrorMessage = "Formats must be one or more of: epub, fb2, mobi, pdf.")]
        [Display(Name = "Available Formats (comma-separated)")]
        public string Formats { get; set; }
    }
}
