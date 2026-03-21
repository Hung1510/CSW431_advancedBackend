using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{
    public class Borrowing
    {
        public int Id { get; set; }

        public int BookId { get; set; }
        public virtual Book Book { get; set; } = null!;

        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public DateTime BorrowedDate { get; set; }

        public DateTime DueDate { get; set; }

        public DateTime? ReturnedDate { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Borrowed"; // Borrowed, Returned, Overdue

        public decimal? LateFee { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;
    }
}
