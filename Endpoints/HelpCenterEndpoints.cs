using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data;
public static class HelpCenterEndpoints
{
       public static void ConfigureHelpCenterEndpoints(this WebApplication app)
    {
        // Register a new help center
        app.MapPost("/helpcenter/register", async (HelpCenterModel model, MySqlConnection db) =>
        {
            // Validate model
            if (!IsValid(model, out var validationErrors))
            {
                return Results.BadRequest(new { Message = "Validation failed.", Errors = validationErrors });
            }
            // Hash the password before storing
            var hashedPassword = HashPassword(model.Password);
            var query = "INSERT INTO HelpCenters (Name, Address, Email, PhoneNumber, Password, Role) VALUES (@Name, @Address, @Email, @PhoneNumber, @Password, @Role)";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Name", model.Name);
            cmd.Parameters.AddWithValue("@Address", model.Address);
            cmd.Parameters.AddWithValue("@Email", model.Email);
            cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
            cmd.Parameters.AddWithValue("@Password", hashedPassword);
            cmd.Parameters.AddWithValue("@Role", "Police"); // Set default role as Police

            await db.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return Results.Ok(new { Message = "Help center registered successfully." });
        });

        // Login Help Center
        app.MapPost("/helpcenter/login", async (HelpCenterModel model, MySqlConnection db) =>
        {
            // Validate model for login
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                return Results.BadRequest(new { Message = "Email and password are required." });
            }

            var query = "SELECT Password FROM HelpCenters WHERE Email = @Email";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Email", model.Email);

            await db.OpenAsync();
            var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var storedPassword = reader.GetString("Password");
                // Here, implement your password verification logic
                if (VerifyPassword(model.Password, storedPassword))
                {
                    return Results.Ok(new { Message = "Hello Police Station!" });
                }
            }

            return Results.Unauthorized();
        });

        // Get all help centers sorted by name
        app.MapGet("/helpcenters", async (MySqlConnection db) =>
        {
            var query = "SELECT * FROM HelpCenters";
            await using var cmd = new MySqlCommand(query, db);
            await db.OpenAsync();
            var reader = await cmd.ExecuteReaderAsync();

            var helpCenters = new List<HelpCenterModel>();
            while (await reader.ReadAsync())
            {
                helpCenters.Add(new HelpCenterModel
                {
                    Name = reader.GetString("Name"),
                    Address = reader.GetString("Address"),
                    Email = reader.GetString("Email"),
                    PhoneNumber = reader.GetString("PhoneNumber"),
                    ConfirmPassword = string.Empty // Not applicable here
                });
            }

            // Sort help centers by Name using LINQ
            var sortedHelpCenters = helpCenters.OrderBy(h => h.Name).ToList();
            return Results.Ok(sortedHelpCenters);
        });

        // Update help center information
        app.MapPut("/helpcenter/{id:int}", async (int id, HelpCenterModel model, MySqlConnection db) =>
        {
            // Validate model
            if (!IsValid(model, out var validationErrors))
            {
                return Results.BadRequest(new { Message = "Validation failed.", Errors = validationErrors });
            }

            var query = "UPDATE HelpCenters SET Name = @Name, Address = @Address, Email = @Email, PhoneNumber = @PhoneNumber WHERE Id = @Id";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Name", model.Name);
            cmd.Parameters.AddWithValue("@Address", model.Address);
            cmd.Parameters.AddWithValue("@Email", model.Email);
            cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
            cmd.Parameters.AddWithValue("@Id", id);

            await db.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0 ? Results.Ok(new { Message = "Help center updated successfully." }) : Results.NotFound(new { Message = "Help center not found." });
        });

        // Delete help center
        app.MapDelete("/helpcenter/{id:int}", async (int id, MySqlConnection db) =>
        {
            var query = "DELETE FROM HelpCenters WHERE Id = @Id";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Id", id);

            await db.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0 ? Results.Ok(new { Message = "Help center deleted successfully." }) : Results.NotFound(new { Message = "Help center not found." });
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

    private static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes); // Store the hashed password
        }
    }

    private static bool VerifyPassword(string enteredPassword, string storedPassword)
    {
        var hashedEnteredPassword = HashPassword(enteredPassword);
        return hashedEnteredPassword == storedPassword; // Compare hashed passwords
    }
}
