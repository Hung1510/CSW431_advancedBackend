using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime MembershipDate { get; set; }

        [MaxLength(200)]
        public string Address { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        //Nav property
        public virtual ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();
    }
}
