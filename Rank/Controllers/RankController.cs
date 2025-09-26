using Microsoft.AspNetCore.Mvc;
using Service.Model;
using Service.Services;


namespace Rank.Controllers
{
    [ApiController]
    [Route("")]
    public class LeaderboardController : ControllerBase
    {
        private readonly ILeaderboardService _service;

        public LeaderboardController(ILeaderboardService service)
        {
            _service = service;
        }

        [HttpPost("customer/{customerid}/score/{score}")]
        public ActionResult<decimal> UpdateScore(long customerid, decimal score)
        {
            try
            {
                var newScore = _service.UpdateScore(customerid, score);
                return Ok(newScore);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("leaderboard")]
        public ActionResult GetLeaderboardByRank([FromQuery] int start, [FromQuery] int end)
        {
            try
            {
                var result = _service.GetByRank(start, end);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("leaderboard/{customerid}")]
        [ProducesResponseType(typeof(IEnumerable<Customer>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GetLeaderboardByCustomer(long customerid, [FromQuery] int high = 0, [FromQuery] int low = 0)
        {
            try
            {
                var result = _service.GetByCustomer(customerid, high, low);
                if (result == null)
                {
                    return NotFound();
                }
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 1百万插入测试  1500ms 左右
        /// </summary>
        /// <returns></returns>
        [HttpPost("test/insert-1m")]
        public IActionResult TestInsert1M()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var random = new Random(42);
            Parallel.For(0L, 1000000L, i =>
            {
                long id = i + 1;
                int deltaInt = random.Next(1, 1001);
                decimal delta = (decimal)deltaInt;
 
                _service.UpdateScore(id, delta);
            });
            stopwatch.Stop();
            int total = _service.GetTotalCount();
            Console.WriteLine($"Inserted 1,000,000 customers in {stopwatch.ElapsedMilliseconds} ms. Total count: {total}");
            return Ok(new { message = "Inserted 1,000,000 customers", timeMs = stopwatch.ElapsedMilliseconds, totalCount = total });
        }

        /// <summary>
        /// 3ms 左右
        /// </summary>
        /// <returns></returns>
        [HttpGet("test/rank-500-10000")]
        public IActionResult TestGetByRank()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = _service.GetByRank(500, 10000);
            stopwatch.Stop();
            Console.WriteLine($"Retrieved 50-50000 customers in {stopwatch.ElapsedMilliseconds} ms. Count: {result.Count}");
            return Ok(new { timeMs = stopwatch.ElapsedMilliseconds, count = result.Count, data = result });
        }

        /// <summary>
        /// 30ms左右
        /// </summary>
        /// <returns></returns>
        [HttpGet("test/customer-neighbors-25000")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult TestGetByCustomer()
        {
            long customerId = 25000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = _service.GetByCustomer(customerId, 1000, 5000);
            stopwatch.Stop();
            if (result == null)
            {
                return NotFound(new { message = "Customer not found or score <= 0", customerId });
            }
            Console.WriteLine($"Retrieved neighbors of customer {customerId} in {stopwatch.ElapsedMilliseconds} ms. Count: {result.Count}");
            return Ok(new { customerId, timeMs = stopwatch.ElapsedMilliseconds, count = result.Count, data = result });
        }
    }
}