using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeClaimApi.Models;

namespace TimeClaimApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TimeClaimsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public TimeClaimsController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }


        [AllowAnonymous]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllClaims()
        {
            var claims = await _db.TimeClaims
                .Select(tc => new
                {
                    tc.Id,
                    tc.Start,
                    tc.End,
                    tc.Message,
                    tc.ImageUrl
                })
                .ToListAsync();

            return Ok(claims);
        }

        [AllowAnonymous]
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableSlots([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            if (to <= from)
                return BadRequest("Invalid time range.");

            var claimed = await _db.TimeClaims
                .Where(tc => tc.Start < to && tc.End > from)
                .Select(tc => new { tc.Start, tc.End })
                .ToListAsync();

            return Ok(new
            {
                From = from,
                To = to,
                Claimed = claimed
            });
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUserClaims()
        {
            var userId = _userManager.GetUserId(User);
            var claims = await _db.TimeClaims
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return Ok(claims);
        }

        [Authorize]
        [HttpPost("claim")]
        public async Task<IActionResult> ClaimTime([FromBody] TimeClaimRequest request)
        {
            if (request.End <= request.Start)
                return BadRequest("End time must be after start time.");

            var userId = _userManager.GetUserId(User);

            var overlaps = await _db.TimeClaims.AnyAsync(tc =>
                request.Start < tc.End && request.End > tc.Start);

            if (overlaps)
                return BadRequest("This time block is already claimed or overlaps with another.");

            var claim = new TimeClaim
            {
                UserId = userId!,
                Start = request.Start,
                End = request.End,
                Message = request.Message,
                ImageUrl = request.ImageUrl
            };

            _db.TimeClaims.Add(claim);
            await _db.SaveChangesAsync();

            return Ok(claim);
        }

        [Authorize]
        [HttpPost("transfer/{claimId}")]
        public async Task<IActionResult> TransferClaim(int claimId, [FromBody] string newOwnerEmail)
        {
            var currentUserId = _userManager.GetUserId(User);

            var claim = await _db.TimeClaims.FirstOrDefaultAsync(tc =>
                tc.Id == claimId && tc.UserId == currentUserId);

            if (claim == null)
                return NotFound("Claim not found or not owned by you.");

            var newOwner = await _userManager.FindByEmailAsync(newOwnerEmail);
            if (newOwner == null)
                return BadRequest("Target user not found.");

            claim.UserId = newOwner.Id;
            await _db.SaveChangesAsync();

            return Ok("Ownership transferred.");
        }
    }

    public class TimeClaimRequest
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string? Message { get; set; }
        public string? ImageUrl { get; set; }
    }
}
