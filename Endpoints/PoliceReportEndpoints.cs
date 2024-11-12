using MySql.Data.MySqlClient;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.IO;

public static class PoliceReportEndpoints
{
    public static void ConfigurePoliceReportEndpoints(this WebApplication app)
    {
        // POST: Submit a police report
        app.MapPost("/postreport", async (HttpRequest request, MySqlConnection db) =>
        {
            var form = await request.ReadFormAsync();

            // Extract form data
            var report = new PoliceReportModel
            {
                FullName = form["FullName"],
                MobileNumber = form["MobileNumber"],
                IncidentType = form["IncidentType"],
                DateTime = DateTime.Now,
                Description = form["Description"],
                Address = form["Address"],
                PoliceStation = form["PoliceStation"],
                UserId = int.Parse(form["UserId"]) // Extract UserId from form data
            };

            // Validate the model
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(report);
            bool isValid = Validator.TryValidateObject(report, validationContext, validationResults, true);

            if (!isValid)
            {
                return Results.BadRequest(new { Message = "Validation failed", Errors = validationResults });
            }

            // Handle file uploads
            var evidenceFilePaths = new List<string>();
            foreach (var file in form.Files)
            {
                if (!IsValidFile(file))
                {
                    return Results.BadRequest(new { Message = "Invalid file type. Only jpg, png, and video files are allowed." });
                }

                var filePath = Path.Combine("EvidenceFiles", file.FileName); // Change "EvidenceFiles" to your desired folder path
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)); // Ensure directory exists

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                evidenceFilePaths.Add(filePath); // Add file path to the list
            }

            var query = "INSERT INTO PoliceReports (FullName, MobileNumber, IncidentType, DateTime, Description, Address, PoliceStation, EvidenceFilePath, UserId) VALUES (@FullName, @MobileNumber, @IncidentType, @DateTime, @Description, @Address, @PoliceStation, @EvidenceFilePath, @UserId)";

            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@FullName", report.FullName);
            cmd.Parameters.AddWithValue("@MobileNumber", report.MobileNumber);
            cmd.Parameters.AddWithValue("@IncidentType", report.IncidentType);
            cmd.Parameters.AddWithValue("@DateTime", DateTime.Now);
            cmd.Parameters.AddWithValue("@Description", report.Description);
            cmd.Parameters.AddWithValue("@Address", report.Address);
            cmd.Parameters.AddWithValue("@PoliceStation", report.PoliceStation);
            cmd.Parameters.AddWithValue("@EvidenceFilePath", string.Join(",", evidenceFilePaths));
            cmd.Parameters.AddWithValue("@UserId", report.UserId); // Add UserId parameter

            await db.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return Results.Ok(new { Message = "Report submitted successfully." });
        });

        // GET: Retrieve all police reports
        app.MapGet("/getreports", async (MySqlConnection db) =>
        {
            var reports = new List<PoliceReportModel>();

            var query = "SELECT Id, UserId, FullName, MobileNumber, IncidentType, DateTime, Description, Address, PoliceStation, EvidenceFilePath FROM PoliceReports";
            await using var cmd = new MySqlCommand(query, db);
            await db.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                reports.Add(new PoliceReportModel
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    FullName = reader.GetString(2),
                    MobileNumber = reader.GetString(3),
                    IncidentType = reader.GetString(4),
                    DateTime = reader.GetDateTime(5),
                    Description = reader.GetString(6),
                    Address = reader.GetString(7),
                    PoliceStation = reader.GetString(8),
                    EvidenceFilePath = reader.GetString(9).Split(',').ToList()
                });
            }

            return Results.Ok(reports);
        });

        // GET: Retrieve police reports by UserId
        app.MapGet("/getreports/{userId:int}", async (int userId, MySqlConnection db) =>
        {
            var reports = new List<PoliceReportModel>();

            var query = "SELECT Id, UserId, FullName, MobileNumber, IncidentType, DateTime, Description, Address, PoliceStation, EvidenceFilePath FROM PoliceReports WHERE UserId = @UserId";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await db.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                reports.Add(new PoliceReportModel
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    FullName = reader.GetString(2),
                    MobileNumber = reader.GetString(3),
                    IncidentType = reader.GetString(4),
                    DateTime = reader.GetDateTime(5),
                    Description = reader.GetString(6),
                    Address = reader.GetString(7),
                    PoliceStation = reader.GetString(8),
                    EvidenceFilePath = reader.GetString(9).Split(',').ToList()
                });
            }

            return Results.Ok(reports);
        });

        // PUT: Update a police report
        app.MapPut("/updatereport/{id}", async (int id, HttpRequest request, MySqlConnection db) =>
        {
            var form = await request.ReadFormAsync();

            // Extract form data
            var report = new PoliceReportModel
            {
                FullName = form["FullName"],
                MobileNumber = form["MobileNumber"],
                IncidentType = form["IncidentType"],
                DateTime = DateTime.Parse(form["DateTime"]),
                Description = form["Description"],
                Address = form["Address"],
                PoliceStation = form["PoliceStation"],
                UserId = int.Parse(form["UserId"]), // Extract UserId from form data
                EvidenceFilePath = new List<string>()
            };

            // Validate the model
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(report);
            bool isValid = Validator.TryValidateObject(report, validationContext, validationResults, true);

            if (!isValid)
            {
                return Results.BadRequest(new { Message = "Validation failed", Errors = validationResults });
            }

            // Handle file uploads
            foreach (var file in form.Files)
            {
                if (!IsValidFile(file))
                {
                    return Results.BadRequest(new { Message = "Invalid file type. Only jpg, png, and video files are allowed." });
                }

                var filePath = Path.Combine("EvidenceFiles", file.FileName); // Change "EvidenceFiles" to your desired folder path
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)); // Ensure directory exists

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                report.EvidenceFilePath.Add(filePath); // Add file path to the list
            }

            var query = "UPDATE PoliceReports SET FullName = @FullName, MobileNumber = @MobileNumber, IncidentType = @IncidentType, DateTime = @DateTime, Description = @Description, Address = @Address, PoliceStation = @PoliceStation, EvidenceFilePath = @EvidenceFilePath, UserId = @UserId WHERE Id = @Id";

            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@FullName", report.FullName);
            cmd.Parameters.AddWithValue("@MobileNumber", report.MobileNumber);
            cmd.Parameters.AddWithValue("@IncidentType", report.IncidentType);
            cmd.Parameters.AddWithValue("@DateTime", DateTime.Now);
            cmd.Parameters.AddWithValue("@Description", report.Description);
            cmd.Parameters.AddWithValue("@Address", report.Address);
            cmd.Parameters.AddWithValue("@PoliceStation", report.PoliceStation);
            cmd.Parameters.AddWithValue("@EvidenceFilePath", string.Join(",", report.EvidenceFilePath ?? new List<string>()));
            cmd.Parameters.AddWithValue("@UserId", report.UserId); // Add UserId parameter

            await db.OpenAsync();
            var result = await cmd.ExecuteNonQueryAsync();

            if (result == 0)
            {
                return Results.NotFound(new { Message = "Report not found." });
            }

            return Results.Ok(new { Message = "Report updated successfully.", Id = id });
        });

        // DELETE: Delete a police report
        app.MapDelete("/deletereport/{id}", async (int id, MySqlConnection db) =>
        {
            var query = "DELETE FROM PoliceReports WHERE Id = @Id";

            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Id", id);

            await db.OpenAsync();
            var result = await cmd.ExecuteNonQueryAsync();

            if (result == 0)
            {
                return Results.NotFound(new { Message = "Report not found." });
            }

            return Results.Ok(new { Message = "Report deleted successfully." });
        });
    }

    private static bool IsValidFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".mp4", ".avi", ".mov" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(extension);
    }
}