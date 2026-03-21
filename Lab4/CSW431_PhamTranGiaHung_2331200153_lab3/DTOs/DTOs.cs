namespace LibraryAPI.DTOs
{
    // Book
    public class BookDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int AvailableCopies { get; set; }
    }

    public class BookDetailDto : BookDto
    {
        public DateTime PublishedDate { get; set; }
        public int TotalCopies { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    //Borrowing
    public class BorrowingDto
    {
        public int Id { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime BorrowedDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? LateFee { get; set; }
    }

    //Statistic
    public class BorrowingStatisticsDto
    {
        public int TotalBorrowings { get; set; }
        public int ActiveBorrowings { get; set; }
        public int OverdueBorrowings { get; set; }
        public int TotalReturned { get; set; }
        public decimal TotalLateFees { get; set; }
        public List<PopularBookDto> MostBorrowedBooks { get; set; } = new();
    }

    public class PopularBookDto
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int BorrowCount { get; set; }
    }

    // Pagination
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
