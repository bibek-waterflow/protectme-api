using System.ComponentModel.DataAnnotations;
using MySql.Data.MySqlClient;

public static class UserEndpoints
{
    public static void ConfigureUserEndpoints(this WebApplication app)
    {
        // Register a new user
        app.MapPost("/registeruser", async (UserRegistrationModel model, MySqlConnection db) =>
        {
            if (!IsValid(model, out var validationErrors))
            {
                return Results.BadRequest(new { Message = "Validation failed.", Errors = validationErrors });
            }

            model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var query = "INSERT INTO Users (FullName, Email, PhoneNumber, Address, Password, Role) VALUES (@FullName, @Email, @PhoneNumber, @Address, @Password, @Role)";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@FullName", model.FullName);
            cmd.Parameters.AddWithValue("@Email", model.Email);
            cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
            cmd.Parameters.AddWithValue("@Address", model.Address);
            cmd.Parameters.AddWithValue("@Password", model.Password);
            cmd.Parameters.AddWithValue("@Role", "Normal User"); // Default role

            await db.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return Results.Ok(new { Message = "User registered successfully." });
        });

        // Register a new help center
        app.MapPost("/registerhelpcenter", async (HelpCenterRegistrationModel model, MySqlConnection db) =>
        {
            // Validate model
            if (!IsValid(model, out var validationErrors))
            {
                return Results.BadRequest(new { Message = "Validation failed.", Errors = validationErrors });
            }

            // Hash the password before storing it
            model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var query = "INSERT INTO HelpCenters (Name, Address, Email, PhoneNumber, Password, Role) VALUES (@Name, @Address, @Email, @PhoneNumber, @Password, @Role)";

            await using var cmd = new MySqlCommand(query, db);

            cmd.Parameters.AddWithValue("@Name", model.Name);
            cmd.Parameters.AddWithValue("@Address", model.Address);
            cmd.Parameters.AddWithValue("@Email", model.Email);
            cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
            cmd.Parameters.AddWithValue("@Password", model.Password);
            cmd.Parameters.AddWithValue("@Role", "Police Station");

            try
            {
                await db.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                return Results.Ok(new { Message = "Help center registered successfully." });
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
        });

        // Register a new admin (for setup purposes)
        app.MapPost("/registeradmin", async (AdminRegistrationModel model, MySqlConnection db) =>
        {
            if (!IsValid(model, out var validationErrors))
            {
                return Results.BadRequest(new { Message = "Validation failed.", Errors = validationErrors });
            }

            // Do not hash the password for admin
            var query = "INSERT INTO Admins (FullName, Email, PhoneNumber, Address, Password, Role) VALUES (@FullName, @Email, @PhoneNumber, @Address, @Password, @Role)";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@FullName", model.FullName);
            cmd.Parameters.AddWithValue("@Email", model.Email);
            cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
            cmd.Parameters.AddWithValue("@Address", model.Address);
            cmd.Parameters.AddWithValue("@Password", model.Password); // Store the plain password
            cmd.Parameters.AddWithValue("@Role", "Admin");

            await db.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return Results.Ok(new { Message = "Admin registered successfully." });
        });

        // Login user (Admin, Normal User, or Help Center)
        app.MapPost("/login", async (LoginModel loginModel, MySqlConnection db) =>
        {
            if (!IsValid(loginModel, out var loginErrors))
            {
                return Results.BadRequest(new { Message = "Validation failed.", Errors = loginErrors });
            }

            // Check Users table
            var query = "SELECT Password, Role FROM Users WHERE Email = @Email";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Email", loginModel.Email);

            await db.OpenAsync();
            var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                // Check HelpCenters table if not found in Users
                await reader.CloseAsync();
                query = "SELECT Password, Role FROM HelpCenters WHERE Email = @Email";
                cmd.CommandText = query;
                reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    // Check Admins table if not found in HelpCenters
                    await reader.CloseAsync();
                    query = "SELECT Password, Role FROM Admins WHERE Email = @Email";
                    cmd.CommandText = query;
                    reader = await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                    {
                        return Results.BadRequest(new { Message = "User not found." });
                    }
                }
            }

            var hashedPassword = reader["Password"].ToString();
            var role = reader["Role"].ToString();

            // For Admins, compare plain text passwords
            if (role == "Admin")
            {
                if (loginModel.Password != hashedPassword)
                {
                    return Results.BadRequest(new { Message = "Invalid password." });
                }
            }
            else
            {
                if (!BCrypt.Net.BCrypt.Verify(loginModel.Password, hashedPassword))
                {
                    return Results.BadRequest(new { Message = "Invalid password." });
                }
            }

            return Results.Ok(new { Message = $"Login successful. Role: {role}" });
        });

        // Update user information
        app.MapPut("/user/{id:int}", async (int id, UserRegistrationModel model, MySqlConnection db) =>
        {
            if (!IsValid(model, out var validationErrors))
            {
                return Results.BadRequest(new { Message = "Validation failed.", Errors = validationErrors });
            }

            var query = "UPDATE Users SET FullName = @FullName, PhoneNumber = @PhoneNumber, Address = @Address WHERE Id = @Id";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@FullName", model.FullName);
            cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
            cmd.Parameters.AddWithValue("@Address", model.Address);
            cmd.Parameters.AddWithValue("@Id", id);

            await db.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0 ? Results.Ok(new { Message = "User updated successfully." }) : Results.NotFound(new { Message = "User not found." });
        });

        // Delete user
        app.MapDelete("/user/{id:int}", async (int id, MySqlConnection db) =>
        {
            var query = "DELETE FROM Users WHERE Id = @Id";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Id", id);

            await db.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0 ? Results.Ok(new { Message = "User deleted successfully." }) : Results.NotFound(new { Message = "User not found." });
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

public class UserRegistrationModel
{
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string Password { get; set; }
}

public class HelpCenterRegistrationModel
{
    public string Name { get; set; }
    public string Address { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Password { get; set; }
}

public class AdminRegistrationModel
{
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string Password { get; set; }
}

public class LoginModel
{
    public string Email { get; set; }
    public string Password { get; set; }
}