using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;


public static class GoogleAuthEndpoints
{
    public static void ConfigureGoogleAuthEndpoints(this WebApplication app)
    {
        // Google Sign-In
        app.MapPost("/google/signin", async ([FromBody] GoogleAuthRequest request, MySqlConnection db) =>
        {
            var payload = await ValidateGoogleTokenAsync(request.IdToken);
            if (payload == null)
            {
                return Results.BadRequest(new { Message = "Invalid Google ID token." });
            }

            // Check if the user already exists in the database
            var query = "SELECT Id, FullName, Email, PhoneNumber, Address, Role FROM Users WHERE Email = @Email";
            await using var cmd = new MySqlCommand(query, db);
            cmd.Parameters.AddWithValue("@Email", payload.Email);

            await db.OpenAsync();
            var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return Results.BadRequest(new { Message = "User does not exist. Please sign up first." });
            }

            // Get user details
            var user = new
            {
                Id = reader["Id"],
                FullName = reader["FullName"],
                Email = reader["Email"],
                PhoneNumber = reader["PhoneNumber"],
                Address = reader["Address"],
                Role = reader["Role"]
            };

            return Results.Ok(new { Message = "Sign-in successful.", User = user });
        });

        // Google Sign-Up
        app.MapPost("/google/signup", async ([FromBody] GoogleSignupRequest request, MySqlConnection db) =>
        {
            var payload = await ValidateGoogleTokenAsync(request.IdToken);
            if (payload == null)
            {
                return Results.BadRequest(new { Message = "Invalid Google ID token." });
            }

            // Check if user already exists
            var checkQuery = "SELECT Id FROM Users WHERE Email = @Email";
            await using var checkCmd = new MySqlCommand(checkQuery, db);
            checkCmd.Parameters.AddWithValue("@Email", payload.Email);

            await db.OpenAsync();
            var checkReader = await checkCmd.ExecuteReaderAsync();
            if (await checkReader.ReadAsync())
            {
                return Results.BadRequest(new { Message = "User already exists. Please sign in instead." });
            }
            await checkReader.CloseAsync();

            // Insert the new user with additional fields
            var insertQuery = "INSERT INTO Users (FullName, Email, PhoneNumber, Address, Password, Role) VALUES (@FullName, @Email, @PhoneNumber, @Address, @Password, @Role)";
            await using var insertCmd = new MySqlCommand(insertQuery, db);
            insertCmd.Parameters.AddWithValue("@FullName", request.FullName);
            insertCmd.Parameters.AddWithValue("@Email", payload.Email); // Use Google email
            insertCmd.Parameters.AddWithValue("@PhoneNumber", request.PhoneNumber);
            insertCmd.Parameters.AddWithValue("@Address", request.Address);
            insertCmd.Parameters.AddWithValue("@Password", request.Password); // Note: Consider hashing the password
            insertCmd.Parameters.AddWithValue("@Role", "Normal User");

            await insertCmd.ExecuteNonQueryAsync();

            return Results.Ok(new { Message = "Sign-up successful. You can now sign in." });
        });
    }

    // Helper function to validate the Google token
    private static async Task<GoogleJsonWebSignature.Payload?> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
            return payload;
        }
        catch
        {
            return null;
        }
    }
}

// Model for Google Sign-In Request
public class GoogleAuthRequest
{
    public string IdToken { get; set; }
}

// Model for Google Sign-Up Request with Additional Fields
public class GoogleSignupRequest
{
    public string IdToken { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string Password { get; set; }
}
