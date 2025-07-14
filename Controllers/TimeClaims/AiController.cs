using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeClaimApi.Models;

namespace TimeClaimApi.Controllers.TimeClaims
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public AiController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("analyze")]
        public IActionResult AnalyzeText([FromBody] AiAnalyzeRequest request)
        {
            var summary = $"[AI] Summary: {request.Text?.Substring(0, Math.Min(50, request.Text.Length))}...";

            return Ok(new AiAnalyzeResponse
            {
                Summary = summary
            });
        }

        // Analyze a user's time claims for patterns (e.g., most claimed project, total hours, etc.)
        [HttpGet("user-summary/{userId}")]
        public async Task<IActionResult> AnalyzeUserClaims(string userId)
        {
            var claims = await _dbContext.TimeClaims
                .Where(tc => tc.UserId == userId)
                .ToListAsync();

            if (claims.Count == 0)
                return NotFound("No time claims found for this user.");

            var totalHours = claims.Sum(tc => tc.Hours);
            var mostClaimedProject = claims
                .GroupBy(tc => tc.ProjectName)
                .OrderByDescending(g => g.Sum(tc => tc.Hours))
                .First().Key;

            var averageHours = claims.Average(tc => tc.Hours);

            var response = new AiUserClaimAnalysisResponse
            {
                UserId = userId,
                TotalClaims = claims.Count,
                TotalHours = totalHours,
                MostClaimedProject = mostClaimedProject,
                AverageHoursPerClaim = averageHours
            };

            return Ok(response);
        }
    }

    public class AiAnalyzeRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class AiAnalyzeResponse
    {
        public string Summary { get; set; } = string.Empty;
    }

    public class AiUserClaimAnalysisResponse
    {
        public string UserId { get; set; } = string.Empty;
        public int TotalClaims { get; set; }
        public double TotalHours { get; set; }
        public string MostClaimedProject { get; set; } = string.Empty;
        public double AverageHoursPerClaim { get; set; }
    }
}