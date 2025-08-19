namespace FitnessTracker.Api.Dtos
{
    public class SetLogDto
    {
        public int Id { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public int Reps { get; set; }
        public float Weight { get; set; }
    }
}