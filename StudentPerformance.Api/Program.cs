using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using AutoMapper;

using StudentPerformance.Api.Data;
using StudentPerformance.Api.Data.Entities; // For your 'User' entity
using StudentPerformance.Api.Services; // For UserService, GradeService, RoleService (if they are here)
using StudentPerformance.Api.Services.Interfaces; // <--- ADD/ENSURE THIS IS PRESENT for SimplePasswordHasher and ILogger (if your ILogger is defined here)
// using StudentPerformance.Api.Utilities; // <--- REMOVE/COMMENT OUT THIS ONE if SimplePasswordHasher is NOT here

using Microsoft.Extensions.Logging; // This is typically for ILogger, usually not in Services.Interfaces
using Microsoft.AspNetCore.Identity;
using StudentPerformance.Api.Utilities; // This provides IPasswordHasher<TUser>


// --- Начало Program.cs ---

var builder = WebApplication.CreateBuilder(args);

// --- 1. Configure Services ---

builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAutoMapper(typeof(Program));

// Register your custom services for Dependency Injection.
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IGradeService, GradeService>();

// --- CORRECTED: Ensure the using for SimplePasswordHasher matches its actual namespace ---
builder.Services.AddScoped<IPasswordHasher<User>, SimplePasswordHasher>();

builder.Services.AddScoped<IRoleService, RoleService>();

builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IGroupService, GroupService>();
// In Program.cs (inside var builder = WebApplication.CreateBuilder(args); block)

// ... existing service registrations ...

builder.Services.AddScoped<IGroupService, GroupService>(); // Already added for GroupsController
builder.Services.AddScoped<ISubjectService, SubjectService>(); // <--- ADD THIS
builder.Services.AddScoped<ISemesterService, SemesterService>(); // <--- ADD THIS
builder.Services.AddScoped<IAssignmentService, AssignmentService>(); // Already added for AssignmentsController
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();

// ... rest of your service registrations ...
// Configure JWT Authentication.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]))
        };
    });

builder.Services.AddAuthorization();

// Configure Swagger/OpenAPI for API documentation.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Student Performance API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


var app = builder.Build();

// --- Data Seeding Section ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // context.Database.Migrate();

        DataSeeder.SeedData(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}
// --- End Data Seeding Section ---


// --- 2. Configure the HTTP request pipeline (Middleware) ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// --- Конец Program.cs ---