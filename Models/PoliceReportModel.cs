using System.ComponentModel.DataAnnotations;

public class PoliceReportModel
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; } // New field for user identification

    [Required]
    [StringLength(100)]
    public string? FullName { get; set; }

    [Required]
    [Phone]
    public string? MobileNumber { get; set; }

    [Required]
    public string? IncidentType { get; set; }

    [Required]
    public DateTime DateTime { get; set; }

    [Required]
    public string? Description { get; set; }

    [Required]
    public string? Address { get; set; }

    [Required]
    public string? PoliceStation { get; set; }

    public List<string>? EvidenceFilePath { get; set; }
}
