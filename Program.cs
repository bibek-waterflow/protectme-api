using Microsoft.OpenApi.Models;
using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "User & Police Report API", Version = "v1" });
});

// Configure MySQL connection from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddTransient<MySqlConnection>(_ => new MySqlConnection(connectionString));
// Add services to the container
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// Add services to the container
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/login";
    });

builder.Services.AddAuthorization();


// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


// Enable Swagger UI for testing and documentation purposes
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "User & Police Report API v1");
    c.RoutePrefix = string.Empty;  // To serve Swagger at the root URL
});
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

// Configure API endpoints
app.ConfigureUserEndpoints();         // Handles user registration and management
app.ConfigurePoliceReportEndpoints(); // Handles police report submission and retrieval
//app.ConfigureHelpCenterEndpoints();   // Handles help center related requests
app.ConfigureGoogleAuthEndpoints();   // Handles Google Sign-In and Sign-Up

app.Run();