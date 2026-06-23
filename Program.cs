
using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.IRepository;
using AvecADeskApi.Repository;
using AvecADeskApi.IRepository;
using AvecADeskApi.LOG;
using AvecADeskApi.Repositories;
using AvecADeskApi.Repositories.Aih;
using AvecADeskApi.Repositories.Commissions;
using AvecADeskApi.Repositories.Courses;
using AvecADeskApi.Repositories.EmailTemplates;
using AvecADeskApi.Repositories.Institutes;
using AvecADeskApi.Repositories.Institutes;
using AvecADeskApi.Repositories.InstituteScrapping;
using AvecADeskApi.Repositories.Invoices;
using AvecADeskApi.Repositories.Members;
using AvecADeskApi.Repositories.PaymentSchedules;
using AvecADeskApi.Repositories.PaymentSchedules;
using AvecADeskApi.Repositories.Reminders;
using AvecADeskApi.Repositories.Students;
using AvecADeskApi.Repositories.Uploads;
using AvecADeskApi.Repositories.UserRoles;
using AvecADeskApi.Repositories.Vendors;
using AvecADeskApi.Repository;
using AvecADeskApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFile("Logs/app-{Date}.txt");

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AvecADesk API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});


var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("JWT Key missing");
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("VendorPortal", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:5175")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<SqlDbHelper>();
builder.Services.AddSingleton<LogHelper>();
builder.Services.AddScoped<IVendorRepository, VendorRepository>();
builder.Services.AddScoped<IInstituteRepository, InstituteRepository>();
builder.Services.AddScoped<IInstituteScrappingRepository, InstituteScrappingRepository>();
builder.Services.AddScoped<IInstituteWebsiteFetcher, InstituteWebsiteFetcher>();
builder.Services.AddScoped<IInstituteScrappingService, InstituteScrappingService>();
builder.Services.AddHttpClient("InstituteScraper", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Accept.ParseAdd(
        "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-AU,en;q=0.9");
});
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IAihRepository, AihRepository>();
builder.Services.AddScoped<ICommissionRepository, CommissionRepository>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IUploadRepository, UploadRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IReminderRepository, ReminderRepository>();
builder.Services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
builder.Services.AddScoped<IMembersRepository, MembersRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IStartStopRepository, StartStopRepository>();


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AvecADesk API v1"));
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("VendorPortal");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();