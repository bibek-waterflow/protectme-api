// // using Microsoft.AspNetCore.Http;
// // using Microsoft.OpenApi.Models;
// // using MySql.Data.MySqlClient;
// // using System.IO;

// // var builder = WebApplication.CreateBuilder(args);

// // // Add services for Swagger
// // builder.Services.AddEndpointsApiExplorer();
// // builder.Services.AddSwaggerGen(c =>
// // {
// //     c.SwaggerDoc("v1", new OpenApiInfo { Title = "Police Report API", Version = "v1" });
// //     c.OperationFilter<FileUploadOperationFilter>(); // Enable file upload in Swagger
// // });

// // // Add MySQL connection string (Update with your MySQL credentials)
// // var connectionString = "Server=localhost;Database=PoliceReportsDB;User=newuser;Password=newpassword;";
// // builder.Services.AddTransient<MySqlConnection>(_ => new MySqlConnection(connectionString));

// // var app = builder.Build();

// // // Enable Swagger
// // app.UseSwagger();
// // app.UseSwaggerUI(c =>
// // {
// //     c.SwaggerEndpoint("/swagger/v1/swagger.json", "Police Report API v1");
// //     c.RoutePrefix = string.Empty; // Swagger at the root
// // });

// // app.UseHttpsRedirection();

// // app.MapPost("/report", async (HttpRequest request, MySqlConnection db) =>
// // {
// //     var form = await request.ReadFormAsync();

// //     // Extract form data
// //     var fullName = form["FullName"].ToString();
// //     var mobileNumber = form["MobileNumber"].ToString();
// //     var incidentType = form["IncidentType"].ToString();
// //     var description = form["Description"].ToString();
// //     var address = form["Address"].ToString();
// //     var policeStation = form["PoliceStation"].ToString();
// //     var evidenceFiles = form.Files; // Handling uploaded files

// //     // Validate required fields
// //     if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(mobileNumber) ||
// //         string.IsNullOrWhiteSpace(incidentType) || 
// //         string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(address) ||
// //         string.IsNullOrWhiteSpace(policeStation))
// //     {
// //         return Results.BadRequest(new { Message = "All fields are required and must not be empty." });
// //     }

// //     // Ensure at least one evidence file is uploaded
// //     if (evidenceFiles.Count == 0)
// //     {
// //         return Results.BadRequest(new { Message = "At least one evidence file (photo/video) is required." });
// //     }

// //     // Validate evidence files (accept jpg, png, and video formats)
// //     var savedFilePaths = new List<string>(); // To store saved file paths
// //     foreach (var file in evidenceFiles)
// //     {
// //         var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
// //         if (extension != ".jpg" && extension != ".png" && !IsVideoFile(extension))
// //         {
// //             return Results.BadRequest(new { Message = $"Invalid file type: {file.FileName}. Only JPG, PNG, or video files are allowed." });
// //         }

// //         // Save the file
// //         var filePath = Path.Combine("EvidenceFiles", file.FileName); // Change "EvidenceFiles" to your desired folder path
// //         Directory.CreateDirectory(Path.GetDirectoryName(filePath)); // Ensure directory exists

// //         using (var stream = new FileStream(filePath, FileMode.Create))
// //         {
// //             await file.CopyToAsync(stream);
// //         }

// //         savedFilePaths.Add(filePath); // Add file path to the list
// //     }

// //     // Save to database with file paths
// //     var query = "INSERT INTO PoliceReports (FullName, MobileNumber, IncidentType, DateTime, Description, Address, PoliceStation, EvidenceFilePath) VALUES (@FullName, @MobileNumber, @IncidentType, @DateTime, @Description, @Address, @PoliceStation, @EvidenceFilePaths)";

// //     await using (var cmd = new MySqlCommand(query, db))
// //     {
// //         cmd.Parameters.AddWithValue("@FullName", fullName);
// //         cmd.Parameters.AddWithValue("@MobileNumber", mobileNumber);
// //         cmd.Parameters.AddWithValue("@IncidentType", incidentType);
// //         cmd.Parameters.AddWithValue("@DateTime", DateTime.Now); // Automatically set the current date and time
// //         cmd.Parameters.AddWithValue("@Description", description);
// //         cmd.Parameters.AddWithValue("@Address", address);
// //         cmd.Parameters.AddWithValue("@PoliceStation", policeStation);
// //         cmd.Parameters.AddWithValue("@EvidenceFilePath", string.Join(",", savedFilePaths)); // Store file paths as a comma-separated string

// //         await db.OpenAsync();
// //         await cmd.ExecuteNonQueryAsync();
// //     }

// //     return Results.Ok(new { Message = "Report submitted successfully" });
// // })
// // .WithName("SubmitPoliceReport")
// // .Accepts<IFormFileCollection>("multipart/form-data")
// // .Produces(200)
// // .Produces(400);

// // // Add a GET method to retrieve police reports
// // app.MapGet("/report", async (MySqlConnection db) =>
// // {
// //     var reports = new List<object>();

// //     var query = "SELECT FullName, MobileNumber, IncidentType, DateTime, Description, Address, PoliceStation, EvidenceFilePath FROM PoliceReports";

// //     await using (var cmd = new MySqlCommand(query, db))
// //     {
// //         await db.OpenAsync();
// //         await using (var reader = await cmd.ExecuteReaderAsync())
// //         {
// //             while (await reader.ReadAsync())
// //             {
// //                 reports.Add(new
// //                 {
// //                     FullName = reader["FullName"].ToString(),
// //                     MobileNumber = reader["MobileNumber"].ToString(),
// //                     IncidentType = reader["IncidentType"].ToString(),
// //                     DateTime = reader["DateTime"].ToString(),
// //                     Description = reader["Description"].ToString(),
// //                     Address = reader["Address"].ToString(),
// //                     PoliceStation = reader["PoliceStation"].ToString(),
// //                     EvidenceFilePath = reader["EvidenceFilePath"] != DBNull.Value ? reader["EvidenceFilePath"].ToString() : null
// //                 });
// //             }
// //         }
// //     }

// //     return Results.Ok(reports);
// // })
// // .WithName("GetPoliceReports")
// // .Produces<List<object>>(200)
// // .Produces(500);


// // // Method to check if the file is a video
// // bool IsVideoFile(string extension)
// // {
// //     return extension == ".mp4" || extension == ".avi" || extension == ".mov" || extension == ".wmv"; // Add more formats as needed
// // }

// // // Run the app
// // app.Run();

// // // Swagger file upload support
// // public class FileUploadOperationFilter : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
// // {
// //     public void Apply(OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
// //     {
// //         if (operation.OperationId == "SubmitPoliceReport")
// //         {
// //             operation.RequestBody = new OpenApiRequestBody
// //             {
// //                 Content = new Dictionary<string, OpenApiMediaType>
// //                 {
// //                     ["multipart/form-data"] = new OpenApiMediaType
// //                     {
// //                         Schema = new OpenApiSchema
// //                         {
// //                             Type = "object",
// //                             Properties = new Dictionary<string, OpenApiSchema>
// //                             {
// //                                 { "FullName", new OpenApiSchema { Type = "string" } },
// //                                 { "MobileNumber", new OpenApiSchema { Type = "string" } },
// //                                 { "IncidentType", new OpenApiSchema { Type = "string" } },
// //                                 { "DateTime", new OpenApiSchema { Type = "string", Format = "date-time" } },
// //                                 { "Description", new OpenApiSchema { Type = "string" } },
// //                                 { "Address", new OpenApiSchema { Type = "string" } },
// //                                 { "PoliceStation", new OpenApiSchema { Type = "string" } },
// //                                 { "Evidence", new OpenApiSchema { Type = "array", Items = new OpenApiSchema { Type = "string", Format = "binary" } } }
// //                             },
// //                             Required = new HashSet<string> { "FullName", "MobileNumber", "IncidentType", "DateTime", "Description", "Address", "PoliceStation" } // Removed "Evidence" from required fields
// //                         }
// //                     }
// //                 }
// //             };
// //         }
// //     }
// // }





// // using Microsoft.OpenApi.Models;
// // using MySql.Data.MySqlClient;
// // using YourNamespace.Endpoints;

// // var builder = WebApplication.CreateBuilder(args);

// // // Add services to the container
// // builder.Services.AddEndpointsApiExplorer();
// // builder.Services.AddSwaggerGen(c =>
// // {
// //     c.SwaggerDoc("v1", new OpenApiInfo { Title = "User Registration API", Version = "v1" });
// // });

// // // Add MySQL connection string
// // var connectionString = "Server=localhost;Database=PoliceReportsDB;User=newuser;Password=newpassword;";
// // builder.Services.AddTransient<MySqlConnection>(_ => new MySqlConnection(connectionString));

// // var app = builder.Build();

// // // Enable Swagger
// // app.UseSwagger();
// // app.UseSwaggerUI(c =>
// // {
// //     c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Registration API v1");
// //     c.RoutePrefix = string.Empty;
// // });

// // // Enable HTTPS redirection
// // app.UseHttpsRedirection();

// // // Configure user-related endpoints
// // app.ConfigureUserEndpoints();

// // // Run the application
// // app.Run();


// using Microsoft.OpenApi.Models;
// using MySql.Data.MySqlClient;

// var builder = WebApplication.CreateBuilder(args);

// // Add services to the container
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1", new OpenApiInfo { Title = "User & Police Report API", Version = "v1" });
// });

// // Configure MySQL connection from appsettings.json
// var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// builder.Services.AddTransient<MySqlConnection>(_ => new MySqlConnection(connectionString));

// var app = builder.Build();

// // Enable Swagger
// app.UseSwagger();
// app.UseSwaggerUI(c =>
// {
//     c.SwaggerEndpoint("/swagger/v1/swagger.json", "User & Police Report API v1");
//     c.RoutePrefix = string.Empty;
// });

// app.UseHttpsRedirection();
// app.ConfigureUserEndpoints();
// app.ConfigurePoliceReportEndpoints();
// app.ConfigureHelpCenterEndpoints();

// app.Run();

using Microsoft.OpenApi.Models;
using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger to include the API documentation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "User & Police Report API", Version = "v1" });
});

// Configure MySQL connection from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddTransient<MySqlConnection>(_ => new MySqlConnection(connectionString));

var app = builder.Build();

// Enable Swagger UI for testing and documentation purposes
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "User & Police Report API v1");
    c.RoutePrefix = string.Empty;  // To serve Swagger at the root URL
});

app.UseHttpsRedirection();

// Configure API endpoints
app.ConfigureUserEndpoints();         // Handles user registration and management
app.ConfigurePoliceReportEndpoints(); // Handles police report submission and retrieval
app.ConfigureHelpCenterEndpoints();   // Handles help center related requests
app.ConfigureGoogleAuthEndpoints();   // Handles Google Sign-In and Sign-Up

app.Run();
