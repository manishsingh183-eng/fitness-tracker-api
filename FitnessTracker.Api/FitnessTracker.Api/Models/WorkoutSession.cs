using System.ComponentModel.DataAnnotations;

namespace FitnessTracker.Api.Models
{
    public class WorkoutSession
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }

        // This is the Foreign Key to link to the User
        public int UserId { get; set; }
        // This is the Navigation Property
        public User? User { get; set; }
    }
}
