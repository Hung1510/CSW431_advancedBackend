using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using System.Diagnostics;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly LibraryContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BooksController> _logger;

        public BooksController(LibraryContext context, IMemoryCache cache, ILogger<BooksController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // unoptimized ver
        //Synchronous, no optimization
        [HttpGet("sync")]
        public IActionResult GetBooksSync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting sync book retrieval");

            //Synchronous call, blocks thread
            var books = _context.Books.ToList();

            stopwatch.Stop();
            _logger.LogInformation($"Sync retrieved {books.Count} books in {stopwatch.ElapsedMilliseconds}ms");

            return Ok(books);
        }

        //Async, no other optimizations
        [HttpGet("baseline")]
        public async Task<IActionResult> GetBooksBaseline()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting baseline book retrieval");

            // Loads entire entity - wasteful
            var books = await _context.Books.ToListAsync();

            stopwatch.Stop();
            _logger.LogInformation($"Baseline retrieved {books.Count} books in {stopwatch.ElapsedMilliseconds}ms");

            return Ok(books);
        }

        //optimized ver

        //Async + Projection (only select needed fields)
        [HttpGet("optimized/projection")]
        public async Task<IActionResult> GetBooksOptimizedProjection()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting optimized projection book retrieval");

            // Only select needed fields
            var books = await _context.Books
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN,
                    Genre = b.Genre,
                    AvailableCopies = b.AvailableCopies
                })
                .ToListAsync();

            stopwatch.Stop();
            _logger.LogInformation($"Optimized projection retrieved {books.Count} books in {stopwatch.ElapsedMilliseconds}ms");

            return Ok(books);
        }

        //Async + Projection + Pagination
        [HttpGet("optimized/paginated")]
        public async Task<IActionResult> GetBooksPaginated([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation($"Starting paginated book retrieval (page {page}, size {pageSize})");

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100; // Max limit

            var totalCount = await _context.Books.CountAsync();

            var books = await _context.Books
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN,
                    Genre = b.Genre,
                    AvailableCopies = b.AvailableCopies
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<BookDto>
            {
                Items = books,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            stopwatch.Stop();
            _logger.LogInformation($"Paginated retrieved {books.Count} books (page {page}) in {stopwatch.ElapsedMilliseconds}ms");

            return Ok(result);
        }

        // Async + Projection + Pagination + Caching
        [HttpGet("optimized/cached")]
        public async Task<IActionResult> GetBooksCached([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var stopwatch = Stopwatch.StartNew();
            var cacheKey = $"books_page_{page}_size_{pageSize}";

            _logger.LogInformation($"Checking cache for {cacheKey}");

            if (!_cache.TryGetValue(cacheKey, out PagedResult<BookDto>? result))
            {
                _logger.LogInformation($"Cache miss for {cacheKey}, fetching from database");

                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var totalCount = await _context.Books.CountAsync();

                var books = await _context.Books
                    .Select(b => new BookDto
                    {
                        Id = b.Id,
                        Title = b.Title,
                        Author = b.Author,
                        ISBN = b.ISBN,
                        Genre = b.Genre,
                        AvailableCopies = b.AvailableCopies
                    })
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                result = new PagedResult<BookDto>
                {
                    Items = books,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

                _cache.Set(cacheKey, result, cacheOptions);
            }
            else
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
            }

            stopwatch.Stop();
            _logger.LogInformation($"Cached response returned in {stopwatch.ElapsedMilliseconds}ms");

            return Ok(result);
        }

        // Get book by ID with details
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookById(int id)
        {
            var book = await _context.Books
                .Where(b => b.Id == id)
                .Select(b => new BookDetailDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN,
                    Genre = b.Genre,
                    PublishedDate = b.PublishedDate,
                    TotalCopies = b.TotalCopies,
                    AvailableCopies = b.AvailableCopies,
                    Description = b.Description,
                    Price = b.Price
                })
                .FirstOrDefaultAsync();

            if (book == null)
                return NotFound();

            return Ok(book);
        }

        //Search by title or author
        [HttpGet("search")]
        public async Task<IActionResult> SearchBooks([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query is required");

            var booksQuery = _context.Books
                .Where(b => b.Title.Contains(query) || b.Author.Contains(query));

            var totalCount = await booksQuery.CountAsync();

            var books = await booksQuery
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN,
                    Genre = b.Genre,
                    AvailableCopies = b.AvailableCopies
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<BookDto>
            {
                Items = books,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(result);
        }

        // Get books by genre
        [HttpGet("genre/{genre}")]
        public async Task<IActionResult> GetBooksByGenre(string genre, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var booksQuery = _context.Books
                .Where(b => b.Genre == genre);

            var totalCount = await booksQuery.CountAsync();

            var books = await booksQuery
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN,
                    Genre = b.Genre,
                    AvailableCopies = b.AvailableCopies
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<BookDto>
            {
                Items = books,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(result);
        }

        // Clear cache
        [HttpPost("clear-cache")]
        public IActionResult ClearCache()
        {
            _logger.LogInformation("Cache clear requested");
            return Ok(new { message = "Cache would be cleared in production" });
        }
    }
}
