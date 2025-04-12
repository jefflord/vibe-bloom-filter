namespace VibeBloomFilter.Models;

/// <summary>
/// Represents a person with dispute-related information
/// </summary>
public class Person
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string DisputeDate { get; set; } = string.Empty;
    public string CreditBureau { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string SSN { get; set; } = string.Empty;
    public string DisputedItemDescription { get; set; } = string.Empty;
    public string DisputeReason { get; set; } = string.Empty;
    public List<string> SupportingDocumentIds { get; set; } = new List<string>();
    public decimal OriginalAmount { get; set; }
    public decimal DisputedAmount { get; set; }
    public string AccountStatusBeforeDispute { get; set; } = string.Empty;
    public string AccountStatusAfterDispute { get; set; } = string.Empty;
}