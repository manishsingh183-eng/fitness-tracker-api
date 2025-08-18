namespace FitnessTracker.Api.Dtos
{
    public class CreateWorkoutSessionDto
    {
        public DateTime Date { get; set; }
        public List<CreateSetLogDto> Sets { get; set; } = new();
    }
}