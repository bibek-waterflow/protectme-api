// using System.ComponentModel.DataAnnotations;

// public class UserRegistrationModel
// {
//     [Required(ErrorMessage = "Full Name is required.")]
//     [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
//     public string FullName { get; set; }

//     [Required(ErrorMessage = "Email is required.")]
//     [EmailAddress(ErrorMessage = "Invalid email format.")]
//     public string Email { get; set; }

//     [Required(ErrorMessage = "Phone Number is required.")]
//     [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone Number must be exactly 10 characters long.")]
//     [Phone(ErrorMessage = "Invalid phone number format.")]
//     public string PhoneNumber { get; set; }

//     [Required(ErrorMessage = "Address is required.")]
//     public string Address { get; set; }

//     [Required(ErrorMessage = "Password is required.")]
//     [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
//     public string Password { get; set; }

//     [Required(ErrorMessage = "Confirm Password is required.")]
//     [Compare("Password", ErrorMessage = "Passwords do not match.")]
//     public string ConfirmPassword { get; set; }
// }


// public class LoginModel
// {
//     [Required(ErrorMessage = "Email is required.")]
//     [EmailAddress(ErrorMessage = "Invalid email format.")]
//     public string Email { get; set; }

//     [Required(ErrorMessage = "Password is required.")]
//     public string Password { get; set; }
// }
