namespace FitnessTracker.Api.Models
{
    public class WorkoutSession
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        // This is the missing property the error is complaining about
        public ICollection<SetLog> Sets { get; set; } = new List<SetLog>();
    }
}