using Microsoft.AspNetCore.Mvc;
using System.Data;
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
    /// Gets a sample of the data for display purposes
    /// </summary>
    /// <param name="count">Maximum number of rows to return</param>
    /// <returns>List of data rows</returns>
    [HttpGet("sample")]
    public IActionResult GetSampleData([FromQuery] int count = 10)
    {
        var data = _bloomFilterService.GetSampleData();
        
        // Convert DataTable to a list of dictionaries for easy JSON serialization
        var result = new List<Dictionary<string, object>>();
        
        // Take only the requested number of rows
        int rowCount = Math.Min(count, data.Rows.Count);
        
        for (int i = 0; i < rowCount; i++)
        {
            var row = data.Rows[i];
            
            // Handle supporting document IDs - convert from comma-separated string to array
            var supportingDocIds = row["SupportingDocumentIds"].ToString();
            var docArray = !string.IsNullOrEmpty(supportingDocIds) 
                ? supportingDocIds.Split(',').Select(id => id.Trim()).ToArray() 
                : new string[0];
            
            var item = new Dictionary<string, object>
            {
                { "Name", row["Name"] },
                { "Address", row["Address"] },
                { "UserId", row["UserId"] },
                { "DisputeDate", row["DisputeDate"] },
                { "CreditBureau", row["CreditBureau"] },
                { "AccountNumber", row["AccountNumber"] },
                { "SSN", row["SSN"] },
                { "DisputedItemDescription", row["DisputedItemDescription"] },
                { "DisputeReason", row["DisputeReason"] },
                { "SupportingDocumentIds", docArray },
                { "OriginalAmount", row["OriginalAmount"] },
                { "DisputedAmount", row["DisputedAmount"] },
                { "AccountStatusBeforeDispute", row["AccountStatusBeforeDispute"] },
                { "AccountStatusAfterDispute", row["AccountStatusAfterDispute"] }
            };
            
            result.Add(item);
        }
        
        // Return data with metadata
        return Ok(new {
            TotalCount = data.Rows.Count,
            DisplayCount = result.Count,
            Data = result,
            LoadedFromFiles = _bloomFilterService.LoadedFromJsonFiles
        });
    }

    /// <summary>
    /// Generates sample JSON files in the Data directory
    /// </summary>
    /// <param name="count">Number of files to generate</param>
    /// <returns>Result containing the paths of the generated files</returns>
    [HttpPost("generate")]
    public IActionResult GenerateSampleFiles([FromQuery] int count = 100)
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
            
            // Get sample data
            var data = _bloomFilterService.GetSampleData();
            var generatedFiles = new List<string>();
            
            // Create JSON files with subsets of the data
            for (int i = 0; i < count; i++)
            {
                // Take a random subset of the data for each file
                var random = new Random(Guid.NewGuid().GetHashCode());
                var rowCount = random.Next(500, 1000); // Between 5 and 15 records per file
                
                // Convert rows to Person objects
                var personList = new List<Dictionary<string, object>>();
                for (int j = 0; j < Math.Min(rowCount, data.Rows.Count); j++)
                {
                    // Get a random row from the dataset
                    var rowIndex = random.Next(0, data.Rows.Count);
                    var row = data.Rows[rowIndex];
                    
                    // Handle supporting document IDs - convert from comma-separated string to array
                    var supportingDocIds = row["SupportingDocumentIds"].ToString();
                    var docArray = !string.IsNullOrEmpty(supportingDocIds) 
                        ? supportingDocIds.Split(',').Select(id => id.Trim()).ToArray() 
                        : new string[0];
                    
                    var item = new Dictionary<string, object>
                    {
                        { "Name", row["Name"] },
                        { "Address", row["Address"] },
                        { "UserId", row["UserId"] },
                        { "DisputeDate", row["DisputeDate"] },
                        { "CreditBureau", row["CreditBureau"] },
                        { "AccountNumber", row["AccountNumber"] },
                        { "SSN", row["SSN"] },
                        { "DisputedItemDescription", row["DisputedItemDescription"] },
                        { "DisputeReason", row["DisputeReason"] },
                        { "SupportingDocumentIds", docArray },
                        { "OriginalAmount", row["OriginalAmount"] },
                        { "DisputedAmount", row["DisputedAmount"] },
                        { "AccountStatusBeforeDispute", row["AccountStatusBeforeDispute"] },
                        { "AccountStatusAfterDispute", row["AccountStatusAfterDispute"] }
                    };
                    personList.Add(item);
                }
                
                // Create a unique filename
                var fileName = $"sample_data_{DateTime.Now:yyyyMMdd}_{i + 1}.json";
                var filePath = Path.Combine(dataDirectory, fileName);
                
                // Serialize and write to file
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var jsonContent = System.Text.Json.JsonSerializer.Serialize(personList, jsonOptions);
                System.IO.File.WriteAllText(filePath, jsonContent);
                
                _logger.LogInformation("Generated file: {FilePath} with {Count} records", filePath, personList.Count);
                generatedFiles.Add(fileName);
            }
            
            return Ok(new { 
                Success = true, 
                Message = $"Successfully generated {generatedFiles.Count} JSON files",
                Files = generatedFiles
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