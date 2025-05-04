namespace WebApplication2.Models
{
    public class CartItem
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public bool IsBorrow { get; set; }
        public decimal PriceBuy { get; set; } 
        public decimal PriceBorrow { get; set; }
        public string CoverImage { get; set; }

    }

}
