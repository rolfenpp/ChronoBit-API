namespace TimeClaimApi.Models
{
    public class TimeClaim
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string? Message { get; set; }
        public string? ImageUrl { get; set; }
    }
}
