using CrowdSage.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CrowdSage.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<CrowdsageDbContext>(options =>
{
    //options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
    options.UseOpenIddict();
});

builder.Services.AddIdentity<CrowdsageUser, IdentityRole>()
            .AddEntityFrameworkStores<CrowdsageDbContext>()
            .AddDefaultTokenProviders();



builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings.
    options.User.AllowedUserNameCharacters =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = false;
});

builder.Services.AddOpenIddict()

    // Register the OpenIddict core components.
    .AddCore(options =>
    {
        // Configure OpenIddict to use the Entity Framework Core stores and models.
        // Note: call ReplaceDefaultEntities() to replace the default OpenIddict entities.
        options.UseEntityFrameworkCore()
               .UseDbContext<CrowdsageDbContext>();

        // Enable Quartz.NET integration.
        //options.UseQuartz();
    })

    // Register the OpenIddict server components.
    .AddServer(options =>
    {
        // Enable the token endpoint.
        options.SetTokenEndpointUris("connect/token");

        // Enable the password flow.
        options.AllowPasswordFlow();

        // Accept anonymous clients (i.e clients that don't send a client_id).
        //options.AcceptAnonymousClients();

        // Register the signing and encryption credentials.
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough();
    })

    // Register the OpenIddict validation components.
    .AddValidation(options =>
    {
        // Import the configuration from the local OpenIddict server instance.
        options.UseLocalServer();

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
    });

// Services
builder.Services.AddScoped<IQuestionsService, QuestionsService>();
builder.Services.AddScoped<IAnswerCommentService, AnswerCommentService>();
builder.Services.AddScoped<IAnswersService, AnswersService>();
builder.Services.AddScoped<IQuestionCommentService, QuestionCommentService>();

builder.Logging.AddConsole();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactClient",
        builder => builder.WithOrigins("https://localhost:51708")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
InitializeDb(app);

app.UseCors("AllowReactClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();

static void InitializeDb(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    using var context = scope.ServiceProvider.GetService<CrowdsageDbContext>();
    if (context == null)
    {
        Console.WriteLine($"Couldn't find Context {nameof(CrowdsageDbContext)} in Dependency Injection, it might be that the database couldn't intialize correctly.");
    }
    context?.Database.Migrate();
}