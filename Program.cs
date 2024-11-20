using Microsoft.OpenApi.Models;
using MySql.Data.MySqlClient;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "User & Police Report API", Version = "v1" });
    c.OperationFilter<SwaggerFileOperationFilter>();  // Register the Swagger operation filter
});

// Configure MySQL connection from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddTransient<MySqlConnection>(_ => new MySqlConnection(connectionString));

// Add services to the container for session and memory caching
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container for authentication
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/login";
    });

// Add authorization services
builder.Services.AddAuthorization();

// Enable CORS for all origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add controllers and JSON serialization settings (optional)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())
    );

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

// Configure API endpoints (Define your custom endpoint configuration methods)
app.ConfigureUserEndpoints();         // Handles user registration and management
app.ConfigurePoliceReportEndpoints(); // Handles police report submission and retrieval
app.ConfigureGoogleAuthEndpoints();   // Handles Google Sign-In and Sign-Up

app.Run();

public class SwaggerFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody?.Content.ContainsKey("multipart/form-data") == true)
        {
            var formFields = new Dictionary<string, OpenApiSchema>();

            // Add form fields based on the model properties
            var parameters = context.MethodInfo.GetParameters();
            foreach (var parameter in parameters)
            {
                var parameterType = parameter.ParameterType;
                if (parameterType.IsClass && parameterType != typeof(string))
                {
                    foreach (var property in parameterType.GetProperties())
                    {
                        formFields[property.Name] = new OpenApiSchema
                        {
                            Type = GetOpenApiType(property.PropertyType),
                            Format = GetOpenApiFormat(property.PropertyType)
                        };
                    }
                }
            }

            // Add 'EvidenceFiles' as a file upload field
            formFields["EvidenceFiles"] = new OpenApiSchema
            {
                Type = "array",
                Items = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                }
            };

            operation.RequestBody.Content["multipart/form-data"].Schema = new OpenApiSchema
            {
                Type = "object",
                Properties = formFields
            };
        }
    }

    private string GetOpenApiType(Type type)
    {
        return type == typeof(int) || type == typeof(long) ? "integer" :
               type == typeof(float) || type == typeof(double) || type == typeof(decimal) ? "number" :
               type == typeof(bool) ? "boolean" :
               type == typeof(DateTime) ? "string" :
               "string"; // Default to string for other types
    }

    private string GetOpenApiFormat(Type type)
    {
        return type == typeof(int) || type == typeof(long) ? "int32" :
               type == typeof(float) || type == typeof(double) || type == typeof(decimal) ? "float" :
               type == typeof(DateTime) ? "date-time" :
               null; // Default to no format for other types
    }
}
