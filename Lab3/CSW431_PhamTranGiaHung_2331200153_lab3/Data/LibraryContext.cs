using Microsoft.EntityFrameworkCore;
using LibraryAPI.Models;

namespace LibraryAPI.Data
{
    public class LibraryContext : DbContext
    {
        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Borrowing> Borrowings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //index
            modelBuilder.Entity<Book>()
                .HasIndex(b => b.Title);

            modelBuilder.Entity<Book>()
                .HasIndex(b => b.ISBN);

            modelBuilder.Entity<Book>()
                .HasIndex(b => b.Author);

            modelBuilder.Entity<Book>()
                .HasIndex(b => b.Genre);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Borrowing>()
                .HasIndex(b => b.Status);

            modelBuilder.Entity<Borrowing>()
                .HasIndex(b => b.BorrowedDate);

            // Configrelation
            modelBuilder.Entity<Borrowing>()
                .HasOne(b => b.Book)
                .WithMany(b => b.Borrowings)
                .HasForeignKey(b => b.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Borrowing>()
                .HasOne(b => b.User)
                .WithMany(u => u.Borrowings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var random = new Random(42);
            var books = new List<Book>();
            var users = new List<User>();
            var borrowings = new List<Borrowing>();

            //Book
            string[] genres = { "book A", "book B", "book C", "book D", "book E", "book F", "book G", "book H", "book I", "book K" };
            string[] authors = { "guy A", "guy B", "guy C", "guy D", "guy E", "guy F", "guy G", "guy H", "guy I", "guy K" };

            for (int i = 1; i <= 500; i++)
            {
                books.Add(new Book
                {
                    Id = i,
                    Title = $"Book Title {i}",
                    Author = authors[random.Next(authors.Length)],
                    ISBN = $"978-{random.Next(1000000000, 1999999999)}",
                    PublishedDate = DateTime.Now.AddYears(-random.Next(1, 50)),
                    Genre = genres[random.Next(genres.Length)],
                    TotalCopies = random.Next(5, 20),
                    AvailableCopies = random.Next(0, 15),
                    Description = $"This is detail of Book {i}.",
                    Price = (decimal)(random.Next(10, 100) + random.NextDouble())
                });
            }

            //User
            for (int i = 1; i <= 200; i++)
            {
                users.Add(new User
                {
                    Id = i,
                    Name = $"User {i}",
                    Email = $"user{i}@library.com",
                    PhoneNumber = $"+1-555-{random.Next(1000, 9999)}",
                    MembershipDate = DateTime.Now.AddDays(-random.Next(1, 1000)),
                    Address = $"{random.Next(1, 999)} Main Street, City {i % 50}",
                    IsActive = random.Next(100) > 10 // 90% active
                });
            }

            //Borrowings
            string[] statuses = {"Borrowed", "Returned", "Overdue" };
            for (int i = 1; i <= 1000; i++)
            {
                var borrowedDate = DateTime.Now.AddDays(-random.Next(1, 365));
                var dueDate = borrowedDate.AddDays(14);
                var status = statuses[random.Next(statuses.Length)];
                DateTime? returnedDate = status == "Returned" ? borrowedDate.AddDays(random.Next(1, 20)) : null;

                borrowings.Add(new Borrowing
                {
                    Id = i,
                    BookId = random.Next(1, 501),
                    UserId = random.Next(1, 201),
                    BorrowedDate = borrowedDate,
                    DueDate = dueDate,
                    ReturnedDate = returnedDate,
                    Status = status,
                    LateFee = status == "Overdue" ? (decimal)(random.Next(5, 50)) : null,
                    Notes = i % 10 == 0 ? $"Note for borrowing {i}" : string.Empty
                });
            }

            modelBuilder.Entity<Book>().HasData(books);
            modelBuilder.Entity<User>().HasData(users);
            modelBuilder.Entity<Borrowing>().HasData(borrowings);
        }
    }
}
