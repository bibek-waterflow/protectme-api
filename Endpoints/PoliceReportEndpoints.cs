using MySql.Data.MySqlClient;
using System.ComponentModel.DataAnnotations;

public static class PoliceReportEndpoints
{
    public static void ConfigurePoliceReportEndpoints(this WebApplication app)
    {
        // POST: Submit a police report
        app.MapPost("/postreport", async (PoliceReportModel report, MySqlConnection db) =>
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(report);
            bool isValid = Validator.TryValidateObject(report, validationContext, validationResults, true);

            if (!isValid)
            {
                return Results.BadRequest(new { Message = "Validation failed", Errors = validationResults });
            }

            var query = "INSERT INTO PoliceReports (UserId, FullName, MobileNumber, IncidentType, DateTime, Description, Address, PoliceStation, EvidenceFilePath) " +
                        "VALUES (@UserId, @FullName, @MobileNumber, @IncidentType, @DateTime, @Description, @Address, @PoliceStation, @EvidenceFilePath)";

            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@UserId", report.UserId);
            cmd.Parameters.AddWithValue("@FullName", report.FullName);
            cmd.Parameters.AddWithValue("@MobileNumber", report.MobileNumber);
            cmd.Parameters.AddWithValue("@IncidentType", report.IncidentType);
            cmd.Parameters.AddWithValue("@DateTime", DateTime.Now);
            cmd.Parameters.AddWithValue("@Description", report.Description);
            cmd.Parameters.AddWithValue("@Address", report.Address);
            cmd.Parameters.AddWithValue("@PoliceStation", report.PoliceStation);
            cmd.Parameters.AddWithValue("@EvidenceFilePath", string.Join(",", report.EvidenceFilePath ?? new List<string>()));

            await db.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return Results.Ok(new { Message = "Report submitted successfully." });
        });

        // GET: Retrieve police reports
        app.MapGet("/getreport", async (MySqlConnection db) =>
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

        // PUT: Update a police report
        app.MapPut("/updatereport/{id}", async (int id, PoliceReportModel report, MySqlConnection db) =>
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(report);
            bool isValid = Validator.TryValidateObject(report, validationContext, validationResults, true);

            if (!isValid)
            {
                return Results.BadRequest(new { Message = "Validation failed", Errors = validationResults });
            }

            var query = "UPDATE PoliceReports SET UserId = @UserId, FullName = @FullName, MobileNumber = @MobileNumber, IncidentType = @IncidentType, DateTime = @DateTime, " +
                        "Description = @Description, Address = @Address, PoliceStation = @PoliceStation, EvidenceFilePath = @EvidenceFilePath WHERE Id = @Id";

            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@UserId", report.UserId);
            cmd.Parameters.AddWithValue("@FullName", report.FullName);
            cmd.Parameters.AddWithValue("@MobileNumber", report.MobileNumber);
            cmd.Parameters.AddWithValue("@IncidentType", report.IncidentType);
            cmd.Parameters.AddWithValue("@DateTime", DateTime.Now);
            cmd.Parameters.AddWithValue("@Description", report.Description);
            cmd.Parameters.AddWithValue("@Address", report.Address);
            cmd.Parameters.AddWithValue("@PoliceStation", report.PoliceStation);
            cmd.Parameters.AddWithValue("@EvidenceFilePath", string.Join(",", report.EvidenceFilePath ?? new List<string>()));

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
}
