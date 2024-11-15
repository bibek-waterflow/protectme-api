using System.ComponentModel.DataAnnotations;
using MySql.Data.MySqlClient;


public static class PoliceReportEndpoints
{
    public static void ConfigurePoliceReportEndpoints(this WebApplication app)
    {
        // Create a new police report
        app.MapPost("/createreport", async (PoliceReportModel model, MySqlConnection db) =>
        {
            if (!IsValid(model, out var validationErrors))
            {
                return Results.BadRequest(new { Message = "Validation failed.", Errors = validationErrors });
            }

            var query = "INSERT INTO PoliceReports (UserId, FullName, MobileNumber, IncidentType, DateTime, Description, Address, PoliceStation, EvidenceFilePath, Status) VALUES (@UserId, @FullName, @MobileNumber, @IncidentType, @DateTime, @Description, @Address, @PoliceStation, @EvidenceFilePath, @Status)";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@UserId", model.UserId);
            cmd.Parameters.AddWithValue("@FullName", model.FullName);
            cmd.Parameters.AddWithValue("@MobileNumber", model.MobileNumber);
            cmd.Parameters.AddWithValue("@IncidentType", model.IncidentType);
            cmd.Parameters.AddWithValue("@DateTime", model.DateTime);
            cmd.Parameters.AddWithValue("@Description", model.Description);
            cmd.Parameters.AddWithValue("@Address", model.Address);
            cmd.Parameters.AddWithValue("@PoliceStation", model.PoliceStation);
            cmd.Parameters.AddWithValue("@EvidenceFilePath", string.Join(",", model.EvidenceFilePath));
            cmd.Parameters.AddWithValue("@Status", "In Progress");

            await db.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return Results.Ok(new { Message = "Police report submitted successfully." });
        });

        // GET: Retrieve all police reports
        app.MapGet("/getreports", async (MySqlConnection db) =>
        {
            try
            {
                var reports = new List<PoliceReportModel>();

                var query = "SELECT Id, UserId, FullName, MobileNumber, IncidentType, DateTime, Description, Address, PoliceStation, EvidenceFilePath, Status FROM PoliceReports";
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
                        EvidenceFilePath = reader.GetString(9).Split(',').ToList(),
                        Status = reader.GetString(10)
                    });
                }

                return Results.Ok(reports);
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
        });

        // GET: Retrieve police reports by UserId
        app.MapGet("/getreports/user/{userId:int}", async (int userId, MySqlConnection db) =>
        {
            try
            {
                var reports = new List<PoliceReportModel>();

                var query = "SELECT Id, UserId, FullName, MobileNumber, IncidentType, DateTime, Description, Address, PoliceStation, EvidenceFilePath, Status FROM PoliceReports WHERE UserId = @UserId";
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
                        EvidenceFilePath = reader.GetString(9).Split(',').ToList(),
                        Status = reader.GetString(10)
                    });
                }

                return Results.Ok(reports);
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
        });

        // GET: Retrieve police reports by HelpCenterName
        app.MapGet("/getreports/helpcenter/{helpCenterName}", async (string helpCenterName, MySqlConnection db) =>
        {
            try
            {
                var reports = new List<PoliceReportModel>();

                var query = "SELECT Id, UserId, FullName, MobileNumber, IncidentType, DateTime, Description, Address, PoliceStation, EvidenceFilePath, Status FROM PoliceReports WHERE PoliceStation = @HelpCenterName";
                await using var cmd = new MySqlCommand(query, db);
                cmd.Parameters.AddWithValue("@HelpCenterName", helpCenterName);
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
                        EvidenceFilePath = reader.GetString(9).Split(',').ToList(),
                        Status = reader.GetString(10)
                    });
                }

                return Results.Ok(reports);
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
        });

        // Update police report
        app.MapPut("/updatereport/{id:int}", async (int id, PoliceReportModel model, MySqlConnection db) =>
        {
            if (!IsValid(model, out var validationErrors))
            {
                return Results.BadRequest(new { Message = "Validation failed.", Errors = validationErrors });
            }

            var query = "UPDATE PoliceReports SET FullName = @FullName, MobileNumber = @MobileNumber, IncidentType = @IncidentType, DateTime = @DateTime, Description = @Description, Address = @Address, PoliceStation = @PoliceStation, EvidenceFilePath = @EvidenceFilePath, Status = @Status WHERE Id = @Id";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@FullName", model.FullName);
            cmd.Parameters.AddWithValue("@MobileNumber", model.MobileNumber);
            cmd.Parameters.AddWithValue("@IncidentType", model.IncidentType);
            cmd.Parameters.AddWithValue("@DateTime", model.DateTime);
            cmd.Parameters.AddWithValue("@Description", model.Description);
            cmd.Parameters.AddWithValue("@Address", model.Address);
            cmd.Parameters.AddWithValue("@PoliceStation", model.PoliceStation);
            cmd.Parameters.AddWithValue("@EvidenceFilePath", string.Join(",", model.EvidenceFilePath));
            cmd.Parameters.AddWithValue("@Status", model.Status);

            await db.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0 ? Results.Ok(new { Message = "Police report updated successfully." }) : Results.NotFound(new { Message = "Police report not found." });
        });

        // Delete police report
        app.MapDelete("/deletereport/{id:int}", async (int id, MySqlConnection db) =>
        {
            var query = "DELETE FROM PoliceReports WHERE Id = @Id";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Id", id);

            await db.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0 ? Results.Ok(new { Message = "Police report deleted successfully." }) : Results.NotFound(new { Message = "Police report not found." });
        });
    }

    private static bool IsValid<T>(T model, out List<string> validationErrors)
    {
        validationErrors = new List<string>();
        var validationContext = new ValidationContext(model, null, null);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, results, true);

        foreach (var validationResult in results)
        {
            validationErrors.Add(validationResult.ErrorMessage);
        }

        return isValid;
    }
}

public class PoliceReportModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string MobileNumber { get; set; }
    public string IncidentType { get; set; }
    public DateTime DateTime { get; set; }
    public string Description { get; set; }
    public string Address { get; set; }
    public string PoliceStation { get; set; }
    public List<string> EvidenceFilePath { get; set; }
    public string Status { get; set; }
}