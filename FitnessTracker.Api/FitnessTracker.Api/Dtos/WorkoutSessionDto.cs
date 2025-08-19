namespace FitnessTracker.Api.Dtos
{
    public class WorkoutSessionDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public List<SetLogDto> Sets { get; set; } = new();
    }
}