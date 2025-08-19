using FitnessTracker.Api.Data;
using FitnessTracker.Api.Dtos;
using FitnessTracker.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "FitnessTracker API", Version = "v1" });
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
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[]{}
        }
    });
});

builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

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

// 2. Build the application
var app = builder.Build();

// 3. Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// 4. Define Endpoints

// AUTHENTICATION ENDPOINTS
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

app.MapPost("/auth/login", async (UserLoginDto request, DataContext context, IConfiguration config) =>
{
    var user = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
    if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        return Results.BadRequest("Invalid username or password.");
    }
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

// WORKOUT ENDPOINTS
app.MapPost("/workouts", async (CreateWorkoutSessionDto request, DataContext context, HttpContext http) =>
{
    var userIdClaim = http.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
    if (userIdClaim == null) return Results.Unauthorized();
    var userId = int.Parse(userIdClaim.Value);
    var user = await context.Users.FindAsync(userId);
    if (user == null) return Results.NotFound("User not found.");

    var newSession = new WorkoutSession { Date = request.Date, User = user };
    var setLogs = request.Sets.Select(setDto => new SetLog
    {
        ExerciseName = setDto.ExerciseName,
        Reps = setDto.Reps,
        Weight = setDto.Weight,
        WorkoutSession = newSession
    }).ToList();
    context.SetLogs.AddRange(setLogs);
    context.WorkoutSessions.Add(newSession);
    await context.SaveChangesAsync();
    var sessionDto = new WorkoutSessionDto
    {
        Id = newSession.Id,
        Date = newSession.Date,
        Sets = setLogs.Select(s => new SetLogDto
        {
            Id = s.Id,
            ExerciseName = s.ExerciseName,
            Reps = s.Reps,
            Weight = s.Weight
        }).ToList()
    };
    return Results.Created($"/workouts/{newSession.Id}", sessionDto);
}).RequireAuthorization();

app.MapGet("/workouts", async (DataContext context, HttpContext http) =>
{
    var userIdClaim = http.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
    if (userIdClaim == null) return Results.Unauthorized();
    var userId = int.Parse(userIdClaim.Value);
    var sessions = await context.WorkoutSessions
        .Where(s => s.UserId == userId)
        .Include(s => s.Sets)
        .OrderByDescending(s => s.Date)
        .ToListAsync();
    var sessionDtos = sessions.Select(s => new WorkoutSessionDto
    {
        Id = s.Id,
        Date = s.Date,
        Sets = s.Sets.Select(set => new SetLogDto
        {
            Id = set.Id,
            ExerciseName = set.ExerciseName,
            Reps = set.Reps,
            Weight = set.Weight
        }).ToList()
    }).ToList();
    return Results.Ok(sessionDtos);
}).RequireAuthorization();

app.MapDelete("/workouts/{id}", async (int id, DataContext context, HttpContext http) =>
{
    var userIdClaim = http.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
    if (userIdClaim == null) return Results.Unauthorized();
    var userId = int.Parse(userIdClaim.Value);
    var session = await context.WorkoutSessions
        .Include(s => s.Sets)
        .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
    if (session == null)
    {
        return Results.NotFound("Workout session not found or you do not have permission to delete it.");
    }
    context.WorkoutSessions.Remove(session);
    await context.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

// 5. Run the application
app.Run();