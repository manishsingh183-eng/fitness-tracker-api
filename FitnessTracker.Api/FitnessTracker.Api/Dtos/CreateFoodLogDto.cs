namespace FitnessTracker.Api.Dtos
{
    public class CreateFoodLogDto
    {
        public string FoodName { get; set; } = string.Empty;
        public float Calories { get; set; }
        public float Protein { get; set; }
        public float Carbs { get; set; }
        public float Fat { get; set; }
        public DateTime DateLogged { get; set; }
    }
}