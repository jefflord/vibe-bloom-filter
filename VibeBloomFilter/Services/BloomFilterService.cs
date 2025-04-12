using System.Data;
using System.Text;
using System.Text.Json;
using BloomFilter;
using BloomFilter.HashAlgorithms;
using Microsoft.Extensions.Logging;
using VibeBloomFilter.Models;

namespace VibeBloomFilter.Services;

/// <summary>
/// Service responsible for generating sample data and managing Bloom filters
/// </summary>
public class BloomFilterService
{
    // Bloom filter for Names column
    private readonly IBloomFilter _nameFilter;
    
    // Bloom filter for Address column
    private readonly IBloomFilter _addressFilter;
    
    // Bloom filter for UserId column
    private readonly IBloomFilter _userIdFilter;
    
    // Bloom filter for Credit Bureau column
    private readonly IBloomFilter _creditBureauFilter;
    
    // Bloom filter for Account Number column
    private readonly IBloomFilter _accountNumberFilter;
    
    // Bloom filter for SSN column
    private readonly IBloomFilter _ssnFilter;
    
    // Bloom filter for Disputed Item Description column
    private readonly IBloomFilter _disputedItemFilter;
    
    // DataTable containing sample data
    private readonly DataTable _sampleData;
    
    // Logger
    private readonly ILogger<BloomFilterService> _logger;
    
    // Flag to indicate if data was loaded from JSON files
    public bool LoadedFromJsonFiles { get; private set; }

    // False positive rate for all filters (1%)
    // This rate was chosen as a good balance between accuracy and memory usage.
    // Lower rates would increase memory usage significantly while higher rates
    // would lead to too many false positives, reducing the utility of the filters.
    private const double FalsePositiveRate = 0.01;
    
    // Expected element count (we know we have 100 items)
    private const int ExpectedElementCount = 100;

    /// <summary>
    /// Constructor: Initializes the service by generating sample data and creating Bloom filters
    /// </summary>
    public BloomFilterService(ILogger<BloomFilterService> logger)
    {
        _logger = logger;
        
        // Create bloom filters with appropriate parameters for each column type
        _nameFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur3());
        
        _addressFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur3());
        
        _userIdFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur3());
            
        _creditBureauFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur3());
            
        _accountNumberFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur3());
            
        _ssnFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur3());
            
        _disputedItemFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur3());
        
        // Load sample data from JSON files
        _logger.LogInformation("Loading sample data from JSON files");
        _sampleData = LoadSampleDataFromJsonFiles();
        
        // If no data was found in JSON files, generate sample data instead
        if (_sampleData.Rows.Count == 0)
        {
            _logger.LogInformation("No data found in JSON files, generating sample data");
            _sampleData = GenerateSampleData();
        }
        else
        {
            _logger.LogInformation("Successfully loaded {RowCount} rows from JSON files", _sampleData.Rows.Count);
        }
        
        // Populate bloom filters with data
        _logger.LogInformation("Populating bloom filters");
        PopulateBloomFilters();
    }

    /// <summary>
    /// Loads data from JSON files in the Data directory
    /// </summary>
    private DataTable LoadSampleDataFromJsonFiles()
    {
        // Create a new DataTable with the specified columns
        var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Address", typeof(string));
        table.Columns.Add("UserId", typeof(int));
        table.Columns.Add("DisputeDate", typeof(string));
        table.Columns.Add("CreditBureau", typeof(string));
        table.Columns.Add("AccountNumber", typeof(string));
        table.Columns.Add("SSN", typeof(string));
        table.Columns.Add("DisputedItemDescription", typeof(string));
        table.Columns.Add("DisputeReason", typeof(string));
        table.Columns.Add("SupportingDocumentIds", typeof(string));
        table.Columns.Add("OriginalAmount", typeof(decimal));
        table.Columns.Add("DisputedAmount", typeof(decimal));
        table.Columns.Add("AccountStatusBeforeDispute", typeof(string));
        table.Columns.Add("AccountStatusAfterDispute", typeof(string));

        try
        {
            // Get the Data directory path
            var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            _logger.LogInformation("Looking for JSON files in directory: {Directory}", dataDirectory);
            
            // If the directory doesn't exist, return empty table (will be populated with generated data)
            if (!Directory.Exists(dataDirectory))
            {
                _logger.LogWarning("Data directory does not exist: {Directory}", dataDirectory);
                return table;
            }
            
            // Get all JSON files in the Data directory
            var jsonFiles = Directory.GetFiles(dataDirectory, "*.json");
            _logger.LogInformation("Found {Count} JSON files in the Data directory", jsonFiles.Length);
            
            // If no files are found, return empty table
            if (jsonFiles.Length == 0)
            {
                _logger.LogWarning("No JSON files found in Data directory");
                return table;
            }
            
            // Read data from each JSON file and add to the DataTable
            int totalRowsAdded = 0;
            foreach (var file in jsonFiles)
            {
                try
                {
                    _logger.LogDebug("Reading file: {FileName}", Path.GetFileName(file));
                    // Read the file content
                    string jsonContent = File.ReadAllText(file);
                    
                    // Deserialize to list of dictionaries
                    var items = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(
                        jsonContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    int rowsAdded = 0;
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            try
                            {
                                // Extract values, handle type conversions
                                string name = item.TryGetValue("Name", out JsonElement nameElement) ? 
                                    nameElement.ToString() : string.Empty;
                                
                                string address = item.TryGetValue("Address", out JsonElement addressElement) ? 
                                    addressElement.ToString() : string.Empty;
                                
                                // Convert UserId from JsonElement to int
                                int userId = 0;
                                if (item.TryGetValue("UserId", out JsonElement userIdElement) && 
                                    userIdElement.ValueKind == JsonValueKind.Number)
                                {
                                    userId = userIdElement.GetInt32();
                                }

                                // Extract additional fields
                                string disputeDate = item.TryGetValue("DisputeDate", out JsonElement disputeDateElement) ? 
                                    disputeDateElement.ToString() : string.Empty;
                                
                                string creditBureau = item.TryGetValue("CreditBureau", out JsonElement creditBureauElement) ? 
                                    creditBureauElement.ToString() : string.Empty;
                                
                                string accountNumber = item.TryGetValue("AccountNumber", out JsonElement accountNumberElement) ? 
                                    accountNumberElement.ToString() : string.Empty;
                                
                                string ssn = item.TryGetValue("SSN", out JsonElement ssnElement) ? 
                                    ssnElement.ToString() : string.Empty;
                                
                                string disputedItemDescription = item.TryGetValue("DisputedItemDescription", out JsonElement disputedItemElement) ? 
                                    disputedItemElement.ToString() : string.Empty;
                                
                                string disputeReason = item.TryGetValue("DisputeReason", out JsonElement disputeReasonElement) ? 
                                    disputeReasonElement.ToString() : string.Empty;
                                
                                // Handle supporting document IDs (could be array or string)
                                string supportingDocIds = string.Empty;
                                if (item.TryGetValue("SupportingDocumentIds", out JsonElement docsElement))
                                {
                                    if (docsElement.ValueKind == JsonValueKind.Array)
                                    {
                                        var docsList = new List<string>();
                                        foreach (var doc in docsElement.EnumerateArray())
                                        {
                                            docsList.Add(doc.ToString());
                                        }
                                        supportingDocIds = string.Join(",", docsList);
                                    }
                                    else
                                    {
                                        supportingDocIds = docsElement.ToString();
                                    }
                                }

                                // Handle decimal amounts
                                decimal originalAmount = 0;
                                if (item.TryGetValue("OriginalAmount", out JsonElement originalAmtElement) &&
                                    originalAmtElement.ValueKind == JsonValueKind.Number)
                                {
                                    originalAmount = originalAmtElement.GetDecimal();
                                }

                                decimal disputedAmount = 0;
                                if (item.TryGetValue("DisputedAmount", out JsonElement disputedAmtElement) &&
                                    disputedAmtElement.ValueKind == JsonValueKind.Number)
                                {
                                    disputedAmount = disputedAmtElement.GetDecimal();
                                }

                                string accountStatusBeforeDispute = item.TryGetValue("AccountStatusBeforeDispute", out JsonElement beforeStatusElement) ? 
                                    beforeStatusElement.ToString() : string.Empty;
                                
                                string accountStatusAfterDispute = item.TryGetValue("AccountStatusAfterDispute", out JsonElement afterStatusElement) ? 
                                    afterStatusElement.ToString() : string.Empty;

                                // Add all extracted values to the row
                                table.Rows.Add(
                                    name,
                                    address,
                                    userId,
                                    disputeDate,
                                    creditBureau,
                                    accountNumber,
                                    ssn,
                                    disputedItemDescription,
                                    disputeReason,
                                    supportingDocIds,
                                    originalAmount,
                                    disputedAmount,
                                    accountStatusBeforeDispute,
                                    accountStatusAfterDispute
                                );
                                
                                rowsAdded++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing item in file {FileName}", Path.GetFileName(file));
                            }
                        }
                    }
                    
                    _logger.LogInformation("Added {RowCount} rows from file {FileName}", rowsAdded, Path.GetFileName(file));
                    totalRowsAdded += rowsAdded;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading JSON file {FileName}", Path.GetFileName(file));
                    // Continue to the next file if one fails
                }
            }
            
            _logger.LogInformation("Total rows added from all JSON files: {TotalRowCount}", totalRowsAdded);
            
            // Set the flag indicating whether data was loaded from files
            LoadedFromJsonFiles = totalRowsAdded > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading JSON files");
            LoadedFromJsonFiles = false;
        }

        return table;
    }

    /// <summary>
    /// Generates a table with 100 sample records
    /// </summary>
    private DataTable GenerateSampleData()
    {
        // Create a new DataTable with the specified columns
        var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Address", typeof(string));
        table.Columns.Add("UserId", typeof(int));
        table.Columns.Add("DisputeDate", typeof(string));
        table.Columns.Add("CreditBureau", typeof(string));
        table.Columns.Add("AccountNumber", typeof(string));
        table.Columns.Add("SSN", typeof(string));
        table.Columns.Add("DisputedItemDescription", typeof(string));
        table.Columns.Add("DisputeReason", typeof(string));
        table.Columns.Add("SupportingDocumentIds", typeof(string));
        table.Columns.Add("OriginalAmount", typeof(decimal));
        table.Columns.Add("DisputedAmount", typeof(decimal));
        table.Columns.Add("AccountStatusBeforeDispute", typeof(string));
        table.Columns.Add("AccountStatusAfterDispute", typeof(string));

        // Sample names, streets, and cities to generate data
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
        
        // Generate 100 random records
        var random = new Random(42); // Using seed for reproducibility
        for (int i = 0; i < 100; i++)
        {
            var firstName = firstNames[random.Next(firstNames.Length)];
            var lastName = lastNames[random.Next(lastNames.Length)];
            var name = $"{firstName} {lastName}";
            
            var streetNumber = random.Next(1, 9999);
            var street = streets[random.Next(streets.Length)];
            var city = cities[random.Next(cities.Length)];
            var address = $"{streetNumber} {street}, {city}";
            
            var userId = 10000 + i;  // Ensure unique user IDs
            
            // Generate data for new fields
            var today = DateTimeOffset.Now;
            var disputeDate = today.AddDays(-random.Next(1, 180)).ToString("yyyy-MM-dd");
            var creditBureau = creditBureaus[random.Next(creditBureaus.Length)];
            var accountNumber = $"{random.Next(1000, 9999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}";
            var ssn = $"{random.Next(100, 999)}-{random.Next(10, 99)}-{random.Next(1000, 9999)}";
            
            var itemIndex = random.Next(disputedItems.Length);
            var disputedItemDescription = disputedItems[itemIndex];
            var disputeReason = disputeReasons[itemIndex]; // Matching reason to item
            
            var docCount = random.Next(1, 4);
            var docIds = new List<string>();
            for (int d = 0; d < docCount; d++)
            {
                docIds.Add($"doc-{random.Next(100, 999)}");
            }
            var supportingDocumentIds = string.Join(",", docIds);
            
            var originalAmount = Math.Round((decimal)random.Next(10, 500) + (decimal)random.NextDouble(), 2);
            var disputedAmount = Math.Round(originalAmount, 2); // Same amount for simplicity, could be modified for partial disputes
            
            var statusBeforeIndex = random.Next(accountStatuses.Length);
            var accountStatusBeforeDispute = accountStatuses[statusBeforeIndex];
            var accountStatusAfterDispute = reviewStatuses[random.Next(reviewStatuses.Length)];
            
            // Add all values to the row
            table.Rows.Add(
                name, 
                address, 
                userId, 
                disputeDate, 
                creditBureau, 
                accountNumber, 
                ssn,
                disputedItemDescription, 
                disputeReason, 
                supportingDocumentIds, 
                originalAmount, 
                disputedAmount,
                accountStatusBeforeDispute, 
                accountStatusAfterDispute
            );
        }

        return table;
    }

    /// <summary>
    /// Iterates through the DataTable and adds each value to the appropriate Bloom filter
    /// </summary>
    private void PopulateBloomFilters()
    {
        foreach (DataRow row in _sampleData.Rows)
        {
            string? name = row["Name"].ToString();
            if (!string.IsNullOrEmpty(name))
            {
                _nameFilter.Add(Encoding.UTF8.GetBytes(name));
            }
            
            string? address = row["Address"].ToString();
            if (!string.IsNullOrEmpty(address))
            {
                _addressFilter.Add(Encoding.UTF8.GetBytes(address));
            }
            
            int userId = (int)row["UserId"];
            _userIdFilter.Add(BitConverter.GetBytes(userId));
            
            // Add new fields to their bloom filters
            string? creditBureau = row["CreditBureau"].ToString();
            if (!string.IsNullOrEmpty(creditBureau))
            {
                _creditBureauFilter.Add(Encoding.UTF8.GetBytes(creditBureau));
            }
            
            string? accountNumber = row["AccountNumber"].ToString();
            if (!string.IsNullOrEmpty(accountNumber))
            {
                _accountNumberFilter.Add(Encoding.UTF8.GetBytes(accountNumber));
            }
            
            string? ssn = row["SSN"].ToString();
            if (!string.IsNullOrEmpty(ssn))
            {
                _ssnFilter.Add(Encoding.UTF8.GetBytes(ssn));
            }
            
            string? disputedItem = row["DisputedItemDescription"].ToString();
            if (!string.IsNullOrEmpty(disputedItem))
            {
                _disputedItemFilter.Add(Encoding.UTF8.GetBytes(disputedItem));
            }
        }
    }

    /// <summary>
    /// Returns the sample DataTable for reference
    /// </summary>
    public DataTable GetSampleData() => _sampleData;

    /// <summary>
    /// Queries all Bloom filters with the provided input
    /// </summary>
    /// <param name="query">String to search for in the Bloom filters</param>
    /// <returns>Dictionary indicating potential matches in each column</returns>
    public Dictionary<string, bool> Query(string query)
    {
        var result = new Dictionary<string, bool>();
        
        // Check for match in Name column
        byte[] nameBytes = Encoding.UTF8.GetBytes(query);
        result["Name"] = _nameFilter.Contains(nameBytes);
        
        // Check for match in Address column
        byte[] addressBytes = Encoding.UTF8.GetBytes(query);
        result["Address"] = _addressFilter.Contains(addressBytes);
        
        // Check for match in UserId column (if query can be parsed as int)
        bool userIdMatch = false;
        if (int.TryParse(query, out int userId))
        {
            byte[] userIdBytes = BitConverter.GetBytes(userId);
            userIdMatch = _userIdFilter.Contains(userIdBytes);
        }
        result["UserId"] = userIdMatch;
        
        // Check for match in Credit Bureau column
        byte[] creditBureauBytes = Encoding.UTF8.GetBytes(query);
        result["CreditBureau"] = _creditBureauFilter.Contains(creditBureauBytes);
        
        // Check for match in Account Number column
        byte[] accountNumberBytes = Encoding.UTF8.GetBytes(query);
        result["AccountNumber"] = _accountNumberFilter.Contains(accountNumberBytes);
        
        // Check for match in SSN column
        byte[] ssnBytes = Encoding.UTF8.GetBytes(query);
        result["SSN"] = _ssnFilter.Contains(ssnBytes);
        
        // Check for match in Disputed Item Description column
        byte[] disputedItemBytes = Encoding.UTF8.GetBytes(query);
        result["DisputedItemDescription"] = _disputedItemFilter.Contains(disputedItemBytes);
        
        return result;
    }
}