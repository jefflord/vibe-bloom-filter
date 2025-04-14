using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text.Json;
using VibeBloomFilter.Services;

namespace VibeBloomFilter.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BloomFilterController : ControllerBase
{
    private readonly BloomFilterService _bloomFilterService;
    private readonly ILogger<BloomFilterController> _logger;

    public BloomFilterController(BloomFilterService bloomFilterService, ILogger<BloomFilterController> logger)
    {
        _bloomFilterService = bloomFilterService;
        _logger = logger;
    }

    /// <summary>
    /// Queries the bloom filters to check if a value might exist in any column
    /// </summary>
    /// <param name="query">Value to search for</param>
    /// <returns>JSON object indicating which columns may contain the value</returns>
    [HttpGet("query")]
    public IActionResult Query([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Query parameter is required");
        }

        _logger.LogInformation("Querying bloom filters with: {Query}", query);
        var result = _bloomFilterService.Query(query);
        
        return Ok(result);
    }

    /// <summary>
    /// Generates sample JSON files in the Data directory
    /// </summary>
    /// <param name="count">Number of files to generate</param>
    /// <returns>Result containing the paths of the generated files</returns>
    [HttpPost("generate")]
    public IActionResult GenerateSampleFiles([FromQuery] int count = 3600)
    {
        try
        {
            _logger.LogInformation("Generating {Count} sample JSON files", count);
            
            // Ensure the Data directory exists
            var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
                _logger.LogInformation("Created Data directory: {Path}", dataDirectory);
            }
            
            var generatedFiles = new List<string>();
            
            // Sample data for generating files
            var firstNames = new[] { "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda", "William", "Elizabeth" };
            var lastNames = new[] { "Smith", "Johnson", "Williams", "Jones", "Brown", "Davis", "Miller", "Wilson", "Moore", "Taylor" };
            var streets = new[] { "Main St", "Oak Ave", "Maple Dr", "Cedar Ln", "Pine Rd", "Elm St", "Washington Ave", "Park Blvd", "Lake Dr", "River Rd" };
            var cities = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose" };
            
            // Sample data for the new fields
            var creditBureaus = new[] { "Experian", "Equifax", "TransUnion" };
            var disputedItems = new[] { 
                "Unauthorized late payment fee", 
                "Incorrect account balance", 
                "Account not mine", 
                "Paid account showing as unpaid", 
                "Incorrect personal information",
                "Account closed still showing active",
                "Duplicate account",
                "Identity theft"
            };
            var disputeReasons = new[] {
                "Payment was made on time via online banking",
                "Statement shows different amount than what was agreed upon",
                "Never opened this account",
                "Have receipt showing payment was made in full",
                "Name is misspelled/wrong address",
                "Account was closed on specified date",
                "Same account reported multiple times",
                "Victim of identity fraud, police report attached"
            };
            var accountStatuses = new[] { "Current", "30 Days Late", "60 Days Late", "90+ Days Late", "Closed", "In Collections", "Charged Off" };
            var reviewStatuses = new[] { "Under Review", "Investigation Complete", "Resolved - In Favor", "Resolved - Against", "Pending Documentation", "Escalated" };
            
            // Create JSON files 
            for (int i = 0; i < count; i++)
            {
                // Create a random generator with a unique seed for each file
                var random = new Random(Guid.NewGuid().GetHashCode());
                var rowCount = random.Next(95, 105); // Approximately 100 records per file
                
                // Generate a list of sample persons
                var personList = new List<Dictionary<string, object>>();
                for (int j = 0; j < rowCount; j++)
                {
                    var firstName = firstNames[random.Next(firstNames.Length)];
                    var lastName = lastNames[random.Next(lastNames.Length)];
                    var name = $"{firstName} {lastName}";
                    
                    var streetNumber = random.Next(1, 9999);
                    var street = streets[random.Next(streets.Length)];
                    var city = cities[random.Next(cities.Length)];
                    var address = $"{streetNumber} {street}, {city}";
                    
                    var userId = 10000 + (i * 1000) + j;  // Ensure unique user IDs
                    
                    // Generate data for new fields
                    var today = DateTimeOffset.Now;
                    var disputeDate = today.AddDays(-random.Next(1, 180)).ToString("yyyy-MM-dd");
                    var creditBureau = creditBureaus[random.Next(creditBureaus.Length)];
                    var accountNumber = $"{random.Next(1000, 9999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}";
                    var ssn = $"{random.Next(100, 999)}-{random.Next(10, 99)}-{random.Next(1000, 9999)}";
                    
                    var itemIndex = random.Next(disputedItems.Length);
                    var disputedItemDescription = disputedItems[itemIndex];
                    var disputeReason = disputeReasons[itemIndex]; // Matching reason to item
                    
                    var statusBeforeIndex = random.Next(accountStatuses.Length);
                    var accountStatusBeforeDispute = accountStatuses[statusBeforeIndex];
                    var accountStatusAfterDispute = reviewStatuses[random.Next(reviewStatuses.Length)];
                    
                    // Generate supporting document IDs
                    var docCount = random.Next(0, 4);
                    var docIds = new string[docCount];
                    for (int d = 0; d < docCount; d++)
                    {
                        docIds[d] = $"DOC-{random.Next(10000, 99999)}";
                    }
                    
                    // Generate random amounts
                    decimal originalAmount = Math.Round((decimal)random.Next(100, 10000) + random.Next(0, 100) / 100.0m, 2);
                    decimal disputedAmount = Math.Round(originalAmount * (decimal)random.NextDouble(), 2);
                    
                    var item = new Dictionary<string, object>
                    {
                        { "Name", name },
                        { "Address", address },
                        { "UserId", userId },
                        { "DisputeDate", disputeDate },
                        { "CreditBureau", creditBureau },
                        { "AccountNumber", accountNumber },
                        { "SSN", ssn },
                        { "DisputedItemDescription", disputedItemDescription },
                        { "DisputeReason", disputeReason },
                        { "SupportingDocumentIds", docIds },
                        { "OriginalAmount", originalAmount },
                        { "DisputedAmount", disputedAmount },
                        { "AccountStatusBeforeDispute", accountStatusBeforeDispute },
                        { "AccountStatusAfterDispute", accountStatusAfterDispute }
                    };
                    personList.Add(item);
                }
                
                // Create a unique filename
                var fileName = $"sample_data_{DateTime.Now:yyyyMMdd}_{i + 1}.json";
                var filePath = Path.Combine(dataDirectory, fileName);
                
                // Serialize and write to file
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var jsonContent = JsonSerializer.Serialize(personList, jsonOptions);
                System.IO.File.WriteAllText(filePath, jsonContent);
                
                _logger.LogInformation("Generated file: {FilePath} with {Count} records", filePath, personList.Count);
                generatedFiles.Add(fileName);
                
                // Clean up to minimize memory usage
                personList.Clear();
                personList = null;
                jsonContent = null;
                
                // Force garbage collection periodically
                if (i % 100 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            
            return Ok(new { 
                Success = true, 
                Message = $"Successfully generated {generatedFiles.Count} JSON files",
                FilesCount = generatedFiles.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sample JSON files");
            return StatusCode(500, new { 
                Success = false, 
                Message = "Error generating sample JSON files", 
                Error = ex.Message 
            });
        }
    }
}