namespace FitnessTracker.Api.Dtos
{
    public class CreateSetLogDto
    {
        public string ExerciseName { get; set; } = string.Empty;
        public int Reps { get; set; }
        public float Weight { get; set; }
    }
}
