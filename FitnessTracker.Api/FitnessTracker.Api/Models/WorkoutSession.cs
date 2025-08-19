namespace FitnessTracker.Api.Models
{
    public class WorkoutSession
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }
        public ICollection<SetLog> Sets { get; set; } = new List<SetLog>();
    }
}