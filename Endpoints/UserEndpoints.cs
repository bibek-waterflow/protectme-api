using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
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

         // Register Help Center
        app.MapPost("/registerhelpcenter", async (HelpCenterRegistrationModel model, MySqlConnection db) =>
        {
            if (!IsValid(model, out var validationErrors))
            {
                return Results.BadRequest(new { Message = "Validation failed.", Errors = validationErrors });
            }

            // Hash the password before storing it
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var query = "INSERT INTO HelpCenters (FullName, Email, PhoneNumber, Address, Password, Role, Latitude, Longitude) VALUES (@FullName, @Email, @PhoneNumber, @Address, @Password, @Role, @Latitude, @Longitude)";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@FullName", model.FullName);
            cmd.Parameters.AddWithValue("@Email", model.Email);
            cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
            cmd.Parameters.AddWithValue("@Address", model.Address);
            cmd.Parameters.AddWithValue("@Password", hashedPassword); // Store the hashed password
            cmd.Parameters.AddWithValue("@Role", "HelpCenter");
            cmd.Parameters.AddWithValue("@Latitude", model.Latitude);
            cmd.Parameters.AddWithValue("@Longitude", model.Longitude);

            await db.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return Results.Ok(new { Message = "Help center registered successfully." });
        });

        // Login user (Admin, Normal User, or Help Center)
        app.MapPost("/login", async (LoginModel loginModel, HttpContext httpContext, MySqlConnection db) =>
        {
            if (!IsValid(loginModel, out var loginErrors))
            {
                return Results.BadRequest(new { Message = "Validation failed.", Errors = loginErrors });
            }

            var query = "SELECT Id, FullName, Email, PhoneNumber, Address, Password, 'Normal User' AS Role FROM Users WHERE Email = @Email";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Email", loginModel.Email);

            await db.OpenAsync();
            MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                // Check HelpCenters table if not found in Users
                await reader.CloseAsync();
                query = "SELECT Id, FullName, Address, Email, PhoneNumber, Password, 'HelpCenter' AS Role FROM HelpCenters WHERE Email = @Email";
                cmd.CommandText = query;
                cmd.Parameters.Clear();  // Clear previous parameters
                cmd.Parameters.AddWithValue("@Email", loginModel.Email);

                reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    // Check Admins table if not found in HelpCenters
                    await reader.CloseAsync();
                    query = "SELECT Id, FullName, Email, PhoneNumber, Address, Password, 'Admin' AS Role FROM Admins WHERE Email = @Email";
                    cmd.CommandText = query;
                    cmd.Parameters.Clear();  // Clear previous parameters
                    cmd.Parameters.AddWithValue("@Email", loginModel.Email);

                    reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                    {
                        return Results.BadRequest(new { Message = "User not found." });
                    }
                }
            }

            var userId = reader.GetInt32(0);
            var fullName = reader.GetString(1);
            var email = reader.GetString(2);
            var phoneNumber = reader.GetString(3);
            var address = reader.GetString(4);
            var hashedPassword = reader.GetString(5);
            var role = reader.GetString(6);

            // Verify password
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

            // Set authentication cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Role, role)
            };
            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            await httpContext.SignInAsync("CookieAuth", claimsPrincipal);

            object user;
            if (role == "HelpCenter")
            {
               
                
                user = new
                {
                    Id = userId,
                    FullName = fullName,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    Address = address,
                   
                    Role = role,
                    Password = (string)null // Do not include the password in the response
                };
            }
            else
            {
                user = new
                {
                    Id = userId,
                    FullName = fullName,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    Address = address,
                    Role = role,
                    Password = (string)null // Do not include the password in the response
                };
            }

            return Results.Ok(new { Message = $"Login successful. Role: {role}", UserDetails = user });
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

        // CRUD operations for Help Centers
        app.MapGet("/helpcenters", async (MySqlConnection db) =>
        {
            var helpCenters = new List<HelpCenterRegistrationModel>();

            var query = "SELECT Id, FullName, Address, Email, PhoneNumber, Latitude, Longitude FROM HelpCenters";
            await using var cmd = new MySqlCommand(query, db);
            await db.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                helpCenters.Add(new HelpCenterRegistrationModel
                {
                    Id = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Address = reader.GetString(2),
                    Email = reader.GetString(3),
                    PhoneNumber = reader.GetString(4),
                    Latitude = reader.GetDouble(5),
                    Longitude = reader.GetDouble(6),
                    Password = null // Do not retrieve passwords for security
                });
            }

            return Results.Ok(helpCenters);
        });

        // Update help center information
        app.MapPut("/helpcenter/{id:int}", async (int id, HelpCenterRegistrationModel model, MySqlConnection db) =>
        {
            if (!IsValid(model, out var validationErrors))
            {
                return Results.BadRequest(new { Message = "Validation failed.", Errors = validationErrors });
            }

            var query = "UPDATE HelpCenters SET FullName = @FullName, Address = @Address, Email = @Email, PhoneNumber = @PhoneNumber, Latitude = @Latitude, Longitude = @Longitude WHERE Id = @Id";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@FullName", model.FullName);
            cmd.Parameters.AddWithValue("@Address", model.Address);
            cmd.Parameters.AddWithValue("@Email", model.Email);
            cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
            cmd.Parameters.AddWithValue("@Latitude", model.Latitude);
            cmd.Parameters.AddWithValue("@Longitude", model.Longitude);

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

        // CRUD operations for Admins
        app.MapGet("/admins", async (MySqlConnection db) =>
        {
            var admins = new List<AdminRegistrationModel>();

            var query = "SELECT Id, FullName, Email, PhoneNumber, Address FROM Admins";
            await using var cmd = new MySqlCommand(query, db);
            await db.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                admins.Add(new AdminRegistrationModel
                {
                    Id = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Email = reader.GetString(2),
                    PhoneNumber = reader.GetString(3),
                    Address = reader.GetString(4),
                    Password = null // Do not retrieve passwords for security
                });
            }

            return Results.Ok(admins);
        });

        app.MapPut("/admin/{id:int}", async (int id, AdminRegistrationModel model, MySqlConnection db) =>
        {
            if (!IsValid(model, out var validationErrors))
            {
                return Results.BadRequest(new { Message = "Validation failed.", Errors = validationErrors });
            }

            var query = "UPDATE Admins SET FullName = @FullName, Email = @Email, PhoneNumber = @PhoneNumber, Address = @Address WHERE Id = @Id";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@FullName", model.FullName);
            cmd.Parameters.AddWithValue("@Email", model.Email);
            cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
            cmd.Parameters.AddWithValue("@Address", model.Address);

            await db.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0 ? Results.Ok(new { Message = "Admin updated successfully." }) : Results.NotFound(new { Message = "Admin not found." });
        });

        app.MapDelete("/admin/{id:int}", async (int id, MySqlConnection db) =>
        {
            var query = "DELETE FROM Admins WHERE Id = @Id";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Id", id);

            await db.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0 ? Results.Ok(new { Message = "Admin deleted successfully." }) : Results.NotFound(new { Message = "Admin not found." });
        });

        // GET: Retrieve all users
        app.MapGet("/users", async (MySqlConnection db) =>
        {
            var users = new List<UserRegistrationModel>();

            var query = "SELECT Id, FullName, Email, PhoneNumber, Address FROM Users";
            await using var cmd = new MySqlCommand(query, db);
            await db.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new UserRegistrationModel
                {
                    Id = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Email = reader.GetString(2),
                    PhoneNumber = reader.GetString(3),
                    Address = reader.GetString(4),
                    Password = null // Do not retrieve passwords for security
                });
            }

            return Results.Ok(users);
        });

        // GET: Retrieve user by ID
        app.MapGet("/user/{id:int}", async (int id, MySqlConnection db) =>
        {
            var query = "SELECT Id, FullName, Email, PhoneNumber, Address FROM Users WHERE Id = @Id";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Id", id);
            await db.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var user = new UserRegistrationModel
                {
                    Id = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Email = reader.GetString(2),
                    PhoneNumber = reader.GetString(3),
                    Address = reader.GetString(4),
                    Password = null // Do not retrieve passwords for security
                };
                return Results.Ok(user);
            }

            return Results.NotFound(new { Message = "User not found." });
        });

        // GET: Retrieve help center by ID
        app.MapGet("/helpcenter/{id:int}", async (int id, MySqlConnection db) =>
        {
            var query = "SELECT Id, Name, Address, Email, PhoneNumber, Latitude, Longitude FROM HelpCenters WHERE Id = @Id";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Id", id);
            await db.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var helpCenter = new HelpCenterRegistrationModel
                {
                    Id = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Address = reader.GetString(2),
                    Email = reader.GetString(3),
                    PhoneNumber = reader.GetString(4),
                    Password = null,
                    Latitude = reader.GetDouble(7),
                    Longitude = reader.GetDouble(8),
                    
                    
                    
                };
                return Results.Ok(helpCenter);
            }

            return Results.NotFound(new { Message = "Help center not found." });
        });

        // GET: Retrieve admin by ID
        app.MapGet("/admin/{id:int}", async (int id, MySqlConnection db) =>
        {
            var query = "SELECT Id, FullName, Email, PhoneNumber, Address FROM Admins WHERE Id = @Id";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Id", id);
            await db.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var admin = new AdminRegistrationModel
                {
                    Id = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Email = reader.GetString(2),
                    PhoneNumber = reader.GetString(3),
                    Address = reader.GetString(4),
                    Password = null // Do not retrieve passwords for security
                };
                return Results.Ok(admin);
            }

            return Results.NotFound(new { Message = "Admin not found." });
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
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Password { get; set; }
}

public class HelpCenterRegistrationModel
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Address { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Password { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class AdminRegistrationModel
{
    public int Id { get; set; }
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