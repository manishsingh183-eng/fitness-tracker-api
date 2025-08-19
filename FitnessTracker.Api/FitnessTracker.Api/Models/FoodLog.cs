namespace FitnessTracker.Api.Models
{
    public class FoodLog
    {
        public int Id { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public float Calories { get; set; }
        public float Protein { get; set; }
        public float Carbs { get; set; }
        public float Fat { get; set; }
        public DateTime DateLogged { get; set; }

        // Foreign Key to the User table
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}