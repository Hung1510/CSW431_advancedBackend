using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data;
using LibraryAPI.DTOs;
using System.Diagnostics;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly LibraryContext _context;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(LibraryContext context, ILogger<StatisticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// Get borrowing statistics - Sequential (Single-threaded)
        [HttpGet("borrowing/sequential")]
        public async Task<IActionResult> GetBorrowingStatisticsSequential()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting sequential statistics calculation");

            //Sequential execution
            var totalBorrowings = await _context.Borrowings.CountAsync();
            var activeBorrowings = await _context.Borrowings.CountAsync(b => b.Status == "Borrowed");
            var overdueBorrowings = await _context.Borrowings.CountAsync(b => b.Status == "Overdue");
            var totalReturned = await _context.Borrowings.CountAsync(b => b.Status == "Returned");
            var totalLateFees = await _context.Borrowings.Where(b => b.LateFee != null).SumAsync(b => b.LateFee ?? 0);

            var mostBorrowed = await _context.Borrowings
                .GroupBy(b => b.BookId)
                .Select(g => new
                {
                    BookId = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .Join(_context.Books,
                    stat => stat.BookId,
                    book => book.Id,
                    (stat, book) => new PopularBookDto
                    {
                        BookId = book.Id,
                        Title = book.Title,
                        Author = book.Author,
                        BorrowCount = stat.Count
                    })
                .ToListAsync();

            var result = new BorrowingStatisticsDto
            {
                TotalBorrowings = totalBorrowings,
                ActiveBorrowings = activeBorrowings,
                OverdueBorrowings = overdueBorrowings,
                TotalReturned = totalReturned,
                TotalLateFees = totalLateFees,
                MostBorrowedBooks = mostBorrowed
            };

            stopwatch.Stop();
            _logger.LogInformation($"Sequential statistics completed in {stopwatch.ElapsedMilliseconds}ms");

            return Ok(new
            {
                data = result,
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                method = "Sequential"
            });
        }

        // Get borrowing statistics - Parallel (Async concurrent execution)
        [HttpGet("borrowing/parallel-async")]
        public async Task<IActionResult> GetBorrowingStatisticsParallelAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting parallel async statistics calculation");

            // Run all queries concurrently using Task.WhenAll
            var totalBorrowingsTask = _context.Borrowings.CountAsync();
            var activeBorrowingsTask = _context.Borrowings.CountAsync(b => b.Status == "Borrowed");
            var overdueBorrowingsTask = _context.Borrowings.CountAsync(b => b.Status == "Overdue");
            var totalReturnedTask = _context.Borrowings.CountAsync(b => b.Status == "Returned");
            var totalLateFeesTask = _context.Borrowings.Where(b => b.LateFee != null).SumAsync(b => b.LateFee ?? 0);
            var mostBorrowedTask = _context.Borrowings
                .GroupBy(b => b.BookId)
                .Select(g => new
                {
                    BookId = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .Join(_context.Books,
                    stat => stat.BookId,
                    book => book.Id,
                    (stat, book) => new PopularBookDto
                    {
                        BookId = book.Id,
                        Title = book.Title,
                        Author = book.Author,
                        BorrowCount = stat.Count
                    })
                .ToListAsync();

            // Wait for all tasks complete
            await Task.WhenAll(
                totalBorrowingsTask,
                activeBorrowingsTask,
                overdueBorrowingsTask,
                totalReturnedTask,
                totalLateFeesTask,
                mostBorrowedTask
            );

            var result = new BorrowingStatisticsDto
            {
                TotalBorrowings = totalBorrowingsTask.Result,
                ActiveBorrowings = activeBorrowingsTask.Result,
                OverdueBorrowings = overdueBorrowingsTask.Result,
                TotalReturned = totalReturnedTask.Result,
                TotalLateFees = totalLateFeesTask.Result,
                MostBorrowedBooks = mostBorrowedTask.Result
            };

            stopwatch.Stop();
            _logger.LogInformation($"Parallel async statistics completed in {stopwatch.ElapsedMilliseconds}ms");

            return Ok(new
            {
                data = result,
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                method = "Parallel Async (Correct for I/O)"
            });
        }

        /// CPU-bound example - Sequential processing
        [HttpGet("cpu-intensive/sequential")]
        public async Task<IActionResult> ProcessDataSequential()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting sequential CPU-intensive processing");

            var books = await _context.Books.Take(100).ToListAsync();
            var results = new List<double>();

            // Sequential CPU processing
            foreach (var book in books)
            {
                var score = CalculateComplexScore(book.Id);
                results.Add(score);
            }

            stopwatch.Stop();
            _logger.LogInformation($"Sequential CPU processing completed in {stopwatch.ElapsedMilliseconds}ms");

            return Ok(new
            {
                processedCount = results.Count,
                averageScore = results.Average(),
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                method = "Sequential"
            });
        }

        // CPU-bound example - Multithreading
        [HttpGet("cpu-intensive/multithreaded")]
        public async Task<IActionResult> ProcessDataMultithreaded()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting multithreaded CPU-intensive processing");

            var books = await _context.Books.Take(100).ToListAsync();
            var results = new System.Collections.Concurrent.ConcurrentBag<double>();

            Parallel.ForEach(books, book =>
            {
                var score = CalculateComplexScore(book.Id);
                results.Add(score);
            });

            stopwatch.Stop();
            _logger.LogInformation($"Multithreaded CPU processing completed in {stopwatch.ElapsedMilliseconds}ms");

            return Ok(new
            {
                processedCount = results.Count,
                averageScore = results.Average(),
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                method = "Multithreaded (Correct for CPU)"
            });
        }

        [HttpGet("borrowing/bad-multithreading")]
        public async Task<IActionResult> GetBorrowingStatisticsBadMultithreading()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting BAD multithreading example");

            var totalBorrowingsTask = Task.Run(async () => await _context.Borrowings.CountAsync());
            var activeBorrowingsTask = Task.Run(async () => await _context.Borrowings.CountAsync(b => b.Status == "Borrowed"));
            var overdueBorrowingsTask = Task.Run(async () => await _context.Borrowings.CountAsync(b => b.Status == "Overdue"));

            await Task.WhenAll(totalBorrowingsTask, activeBorrowingsTask, overdueBorrowingsTask);

            stopwatch.Stop();
            _logger.LogInformation($"Bad multithreading completed in {stopwatch.ElapsedMilliseconds}ms");

            return Ok(new
            {
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                method = "Task.Run for I/O (BAD - adds overhead)",
                note = "This is slower than parallel async because it creates unnecessary threads"
            });
        }

        // Get user activity statistics
        [HttpGet("users/activity")]
        public async Task<IActionResult> GetUserActivity()
        {
            var activeUsers = await _context.Users
                .Where(u => u.IsActive)
                .CountAsync();

            var totalUsers = await _context.Users.CountAsync();

            var topBorrowers = await _context.Borrowings
                .GroupBy(b => b.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    BorrowCount = g.Count()
                })
                .OrderByDescending(x => x.BorrowCount)
                .Take(10)
                .Join(_context.Users,
                    stat => stat.UserId,
                    user => user.Id,
                    (stat, user) => new
                    {
                        userId = user.Id,
                        userName = user.Name,
                        email = user.Email,
                        borrowCount = stat.BorrowCount
                    })
                .ToListAsync();

            return Ok(new
            {
                activeUsers,
                totalUsers,
                topBorrowers
            });
        }

        /// Simulate a CPU calculation
        private double CalculateComplexScore(int bookId)
        {
            // Simulate CPU-intensive work
            double result = 0;
            for (int i = 0; i < 1000000; i++)
            {
                result += Math.Sqrt(bookId * i) * Math.Sin(i) * Math.Cos(bookId);
            }
            return result;
        }

        // Get monthly borrowing trends
        [HttpGet("trends/monthly")]
        public async Task<IActionResult> GetMonthlyTrends()
        {
            var trends = await _context.Borrowings
                .GroupBy(b => new { b.BorrowedDate.Year, b.BorrowedDate.Month })
                .Select(g => new
                {
                    year = g.Key.Year,
                    month = g.Key.Month,
                    totalBorrowings = g.Count(),
                    returned = g.Count(b => b.Status == "Returned"),
                    overdue = g.Count(b => b.Status == "Overdue")
                })
                .OrderByDescending(x => x.year)
                .ThenByDescending(x => x.month)
                .Take(12)
                .ToListAsync();

            return Ok(trends);
        }
    }
}
