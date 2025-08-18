using FitnessTracker.Api.Data;
using Microsoft.EntityFrameworkCore;
using FitnessTracker.Api.Dtos;
using FitnessTracker.Api.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[]{}
        }
    });
});
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Add Authentication and Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();



// AUTHENTICATION ENDPOINTS

// Endpoint for User Registration
app.MapPost("/auth/register", async (UserRegisterDto request, DataContext context) =>
{
    var newUser = new User
    {
        Username = request.Username,
        Email = request.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
    };

    context.Users.Add(newUser);
    await context.SaveChangesAsync();

    return Results.Ok(newUser);
});

// Endpoint for User Login
// Endpoint for User Login
app.MapPost("/auth/login", async (UserLoginDto request, DataContext context, IConfiguration config) =>
{
    var user = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

    if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        return Results.BadRequest("Invalid username or password.");
    }

    // User is valid, create a JWT
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.GetSection("AppSettings:Token").Value!));

    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.Now.AddDays(1),
        SigningCredentials = creds
    };

    var tokenHandler = new JwtSecurityTokenHandler();

    var token = tokenHandler.CreateToken(tokenDescriptor);

    return Results.Ok(new { token = tokenHandler.WriteToken(token) });
});

// PROTECTED ENDPOINT
app.MapGet("/workouts", () => "List of workouts for the authenticated user.")
    .RequireAuthorization();

// WORKOUT ENDPOINTS

app.MapPost("/workouts", async (CreateWorkoutSessionDto request, DataContext context, HttpContext http) =>
{
    // 1. Get the user's ID from the token claims
    var userIdClaim = http.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
    if (userIdClaim == null)
    {
        return Results.Unauthorized();
    }

    var userId = int.Parse(userIdClaim.Value);
    var user = await context.Users.FindAsync(userId);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    // 2. Create the main WorkoutSession object
    var newSession = new WorkoutSession
    {
        Date = request.Date,
        User = user // Link the session to the authenticated user
    };

    // 3. Create the SetLog objects from the DTO and link them to the session
    foreach (var setDto in request.Sets)
    {
        var newSet = new SetLog
        {
            ExerciseName = setDto.ExerciseName,
            Reps = setDto.Reps,
            Weight = setDto.Weight,
            WorkoutSession = newSession // Link the set to the new session
        };
        context.SetLogs.Add(newSet);
    }

    // 4. Add the session itself and save everything to the database
    context.WorkoutSessions.Add(newSession);
    await context.SaveChangesAsync();

    return Results.Created($"/workouts/{newSession.Id}", newSession);
}).RequireAuthorization(); // 5. Protect this endpoint!

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
