namespace VibeBloomFilter.Models;

/// <summary>
/// Represents a person with basic identifying information
/// </summary>
public class Person
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int UserId { get; set; }
}