using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Author { get; set; } = string.Empty;

        [MaxLength(13)]
        public string ISBN { get; set; } = string.Empty;

        public DateTime PublishedDate { get; set; }

        [MaxLength(50)]
        public string Genre { get; set; } = string.Empty;

        public int TotalCopies { get; set; }

        public int AvailableCopies { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        // Nav property
        public virtual ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();
    }
}
