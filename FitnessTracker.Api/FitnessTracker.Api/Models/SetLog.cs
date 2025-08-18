namespace FitnessTracker.Api.Models
{
    public class SetLog
    {
        public int Id { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public int Reps { get; set; }
        public float Weight { get; set; }

        // Foreign Key to link to the WorkoutSession
        public int WorkoutSessionId { get; set; }
        // Navigation Property
        public WorkoutSession? WorkoutSession { get; set; }
    }
}