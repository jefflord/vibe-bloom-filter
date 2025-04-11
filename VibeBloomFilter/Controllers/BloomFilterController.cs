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
            var item = new Dictionary<string, object>
            {
                { "Name", row["Name"] },
                { "Address", row["Address"] },
                { "UserId", row["UserId"] }
            };
            result.Add(item);
        }
        
        return Ok(result);
    }
}