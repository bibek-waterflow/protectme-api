using System.ComponentModel.DataAnnotations;
using System.Data;
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

        // Retrieve all police reports
        app.MapGet("/getreports", async (MySqlConnection db) =>
        {
            var reports = await RetrieveReports("SELECT * FROM PoliceReports", db);
            return Results.Ok(reports);
        });

        // Retrieve police reports by UserId
        app.MapGet("/getreports/user/{userId:int}", async (int userId, MySqlConnection db) =>
        {
            var reports = await RetrieveReports("SELECT * FROM PoliceReports WHERE UserId = @UserId", db, new MySqlParameter("@UserId", userId));
            return Results.Ok(reports);
        });

        // Retrieve police reports by PoliceStation
        app.MapGet("/getreports/helpcenter/{helpCenterName}", async (string helpCenterName, MySqlConnection db) =>
        {
            var reports = await RetrieveReports("SELECT * FROM PoliceReports WHERE PoliceStation = @HelpCenterName", db, new MySqlParameter("@HelpCenterName", helpCenterName));
            return Results.Ok(reports);
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


// Endpoint to Mark Report as Solved (Automatic Status Update)
app.MapPost("/markassolved/{id:int}", async (int id, MySqlConnection db) =>
{
    var query = "UPDATE PoliceReports SET Status = @Status WHERE Id = @Id";
    await using var cmd = new MySqlCommand(query, db);
    cmd.Parameters.AddWithValue("@Id", id);
    cmd.Parameters.AddWithValue("@Status", "Solved");

    await db.OpenAsync();
    var rowsAffected = await cmd.ExecuteNonQueryAsync();
    return rowsAffected > 0
        ? Results.Ok(new { Message = "Status updated to Solved." })
        : Results.NotFound(new { Message = "Police report not found." });
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

    private static async Task<List<PoliceReportModel>> RetrieveReports(string query, MySqlConnection db, params MySqlParameter[] parameters)
    {
        var reports = new List<PoliceReportModel>();
        await using var cmd = new MySqlCommand(query, db);
        
        foreach (var parameter in parameters)
        {
            cmd.Parameters.Add(parameter);
        }

        await db.OpenAsync();

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            reports.Add(new PoliceReportModel
            {
                Id = reader.GetInt32("Id"),
                UserId = reader.GetInt32("UserId"),
                FullName = reader.GetString("FullName"),
                MobileNumber = reader.GetString("MobileNumber"),
                IncidentType = reader.GetString("IncidentType"),
                DateTime = reader.GetDateTime("DateTime"),
                Description = reader.GetString("Description"),
                Address = reader.GetString("Address"),
                PoliceStation = reader.GetString("PoliceStation"),
                EvidenceFilePath = reader.GetString("EvidenceFilePath").Split(',').ToList(),
                Status = reader.GetString("Status")
            });
        }

        return reports;
    }

    private static bool IsValid<T>(T model, out List<string> validationErrors)
    {
        validationErrors = new List<string>();
        var validationContext = new ValidationContext(model);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, validationContext, results, true);

        validationErrors.AddRange(results.Select(result => result.ErrorMessage));
        return isValid;
    }
}

public class PoliceReportModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    [Required]
    public string FullName { get; set; }
    [Required]
    [Phone]
    public string MobileNumber { get; set; }
    [Required]
    public string IncidentType { get; set; }
    public DateTime DateTime { get; set; }
    public string Description { get; set; }
    public string Address { get; set; }
    public string PoliceStation { get; set; }
    public List<string> EvidenceFilePath { get; set; } = new List<string>();
    public string Status { get; set; }
}
