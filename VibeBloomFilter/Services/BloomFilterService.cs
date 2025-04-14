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
    
    // Bloom filter for Dispute Reason column
    private readonly IBloomFilter _disputeReasonFilter;
    
    // Bloom filter for Account Status Before Dispute column
    private readonly IBloomFilter _statusBeforeFilter;
    
    // Bloom filter for Account Status After Dispute column
    private readonly IBloomFilter _statusAfterFilter;
    
    // Bloom filter for Dispute Date column
    private readonly IBloomFilter _disputeDateFilter;
    
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
    private const int ExpectedElementCount = 2000000;

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
            hashFunction: new Murmur32BitsX86());
        
        _addressFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur32BitsX86());
        
        _userIdFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur32BitsX86());
            
        _creditBureauFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur32BitsX86());
            
        _accountNumberFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur32BitsX86());
            
        _ssnFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur32BitsX86());
            
        _disputedItemFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur32BitsX86());
        
        _disputeReasonFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur32BitsX86());
        
        _statusBeforeFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur32BitsX86());
        
        _statusAfterFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur32BitsX86());
        
        _disputeDateFilter = FilterBuilder.Build(
            expectedElements: ExpectedElementCount, 
            errorRate: FalsePositiveRate,
            hashFunction: new Murmur32BitsX86());
        
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


        _sampleData.Dispose();
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
        // Define common stop words to exclude from the bloom filter
        HashSet<string> stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "and", "or", "but", "is", "are", "was", "were", "be", "been", "being", 
            "in", "on", "at", "to", "for", "with", "by", "about", "against", "between", "into", "through", 
            "during", "before", "after", "above", "below", "from", "of", "up", "down", "out", "off", "over", 
            "under", "again", "further", "then", "once", "here", "there", "when", "where", "why", "how", 
            "all", "any", "both", "each", "few", "more", "most", "other", "some", "such", "no", "nor", 
            "not", "only", "own", "same", "so", "than", "too", "very", "can", "will", "just", "should", 
            "now", "this", "that", "these", "those", "i", "me", "my", "myself", "we", "our", "ours", 
            "ourselves", "you", "your", "yours", "yourself", "yourselves", "he", "him", "his", "himself", 
            "she", "her", "hers", "herself", "it", "its", "itself", "they", "them", "their", "theirs", 
            "themselves", "what", "which", "who", "whom", "whose", "as", "if", "because", "until", "while", 
            "have", "has", "had", "do", "does", "did", "could", "would", "via"
        };

        // Helper function to add words to filter
        void AddWordsToFilter(IBloomFilter filter, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            // Convert to lowercase for case-insensitive filtering
            string lowercaseText = text.ToLowerInvariant();
            
            // Add the full text first
            filter.Add(Encoding.UTF8.GetBytes(lowercaseText));

            // If text contains multiple words, add individual words separately
            string[] words = lowercaseText.Split(new[] { ' ', ',', '.', '-', ':', ';', '/', '\\', '(', ')', '[', ']', '{', '}' }, 
                StringSplitOptions.RemoveEmptyEntries);
                
            if (words.Length > 1)
            {
                foreach (string word in words)
                {
                    // Skip stop words
                    if (!stopWords.Contains(word) && word.Length > 1)
                    {
                        filter.Add(Encoding.UTF8.GetBytes(word));
                    }
                }
            }
        }
        
        foreach (DataRow row in _sampleData.Rows)
        {
            string? name = row["Name"].ToString();
            if (!string.IsNullOrEmpty(name))
            {
                AddWordsToFilter(_nameFilter, name);
            }
            
            string? address = row["Address"].ToString();
            if (!string.IsNullOrEmpty(address))
            {
                AddWordsToFilter(_addressFilter, address);
            }
            
            int userId = (int)row["UserId"];
            _userIdFilter.Add(BitConverter.GetBytes(userId));
            
            // Add new fields to their bloom filters
            string? creditBureau = row["CreditBureau"].ToString();
            if (!string.IsNullOrEmpty(creditBureau))
            {
                AddWordsToFilter(_creditBureauFilter, creditBureau);
            }
            
            string? accountNumber = row["AccountNumber"].ToString();
            if (!string.IsNullOrEmpty(accountNumber))
            {
                AddWordsToFilter(_accountNumberFilter, accountNumber);
            }
            
            string? ssn = row["SSN"].ToString();
            if (!string.IsNullOrEmpty(ssn))
            {
                AddWordsToFilter(_ssnFilter, ssn);
            }
            
            string? disputedItem = row["DisputedItemDescription"].ToString();
            if (!string.IsNullOrEmpty(disputedItem))
            {
                AddWordsToFilter(_disputedItemFilter, disputedItem);
            }
            
            string? disputeReason = row["DisputeReason"].ToString();
            if (!string.IsNullOrEmpty(disputeReason))
            {
                AddWordsToFilter(_disputeReasonFilter, disputeReason);
            }
            
            string? statusBefore = row["AccountStatusBeforeDispute"].ToString();
            if (!string.IsNullOrEmpty(statusBefore))
            {
                AddWordsToFilter(_statusBeforeFilter, statusBefore);
            }
            
            string? statusAfter = row["AccountStatusAfterDispute"].ToString();
            if (!string.IsNullOrEmpty(statusAfter))
            {
                AddWordsToFilter(_statusAfterFilter, statusAfter);
            }
            
            string? disputeDate = row["DisputeDate"].ToString();
            if (!string.IsNullOrEmpty(disputeDate))
            {
                AddWordsToFilter(_disputeDateFilter, disputeDate);
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
        
        if (string.IsNullOrWhiteSpace(query))
        {
            // Initialize all results to false if query is empty
            result["Name"] = false;
            result["Address"] = false;
            result["UserId"] = false;
            result["CreditBureau"] = false;
            result["AccountNumber"] = false;
            result["SSN"] = false;
            result["DisputedItemDescription"] = false;
            result["DisputeReason"] = false;
            result["AccountStatusBeforeDispute"] = false;
            result["AccountStatusAfterDispute"] = false;
            result["DisputeDate"] = false;
            return result;
        }
        
        // Define stop words to exclude from search
        HashSet<string> stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "and", "or", "but", "is", "are", "was", "were", "be", "been", "being", 
            "in", "on", "at", "to", "for", "with", "by", "about", "against", "between", "into", "through", 
            "during", "before", "after", "above", "below", "from", "of", "up", "down", "out", "off", "over", 
            "under", "again", "further", "then", "once", "here", "there", "when", "where", "why", "how", 
            "all", "any", "both", "each", "few", "more", "most", "other", "some", "such", "no", "nor", 
            "not", "only", "own", "same", "so", "than", "too", "very", "can", "will", "just", "should", 
            "now", "this", "that", "these", "those", "i", "me", "my", "myself", "we", "our", "ours", 
            "ourselves", "you", "your", "yours", "yourself", "yourselves", "he", "him", "his", "himself", 
            "she", "her", "hers", "herself", "it", "its", "itself", "they", "them", "their", "theirs", 
            "themselves", "what", "which", "who", "whom", "whose", "as", "if", "because", "until", "while", 
            "have", "has", "had", "do", "does", "did", "could", "would", "via"
        };
        
        // Convert query to lowercase for case-insensitive search
        string lowercaseQuery = query.ToLowerInvariant();
        
        // Helper function to check if any word from a multi-word query matches in a filter
        bool CheckMultiWordQuery(IBloomFilter filter, string queryText)
        {
            // First, check if the full phrase matches
            byte[] fullQueryBytes = Encoding.UTF8.GetBytes(queryText);
            bool fullMatch = filter.Contains(fullQueryBytes);
            
            if (fullMatch)
                return true;
                
            // Split into words and check each non-stop word
            string[] words = queryText.Split(new[] { ' ', ',', '.', '-', ':', ';', '/', '\\', '(', ')', '[', ']', '{', '}' }, 
                StringSplitOptions.RemoveEmptyEntries);
                
            if (words.Length > 1)
            {
                foreach (string word in words)
                {
                    // Skip stop words and single characters
                    if (!stopWords.Contains(word) && word.Length > 1)
                    {
                        byte[] wordBytes = Encoding.UTF8.GetBytes(word);
                        if (filter.Contains(wordBytes))
                            return true;
                    }
                }
                return false;
            }
            
            return fullMatch;
        }
        
        // Check for match in Name column
        result["Name"] = CheckMultiWordQuery(_nameFilter, lowercaseQuery);
        
        // Check for match in Address column
        result["Address"] = CheckMultiWordQuery(_addressFilter, lowercaseQuery);
        
        // Check for match in UserId column (if query can be parsed as int)
        bool userIdMatch = false;
        if (int.TryParse(query, out int userId))
        {
            byte[] userIdBytes = BitConverter.GetBytes(userId);
            userIdMatch = _userIdFilter.Contains(userIdBytes);
        }
        result["UserId"] = userIdMatch;
        
        // Check for match in Credit Bureau column
        result["CreditBureau"] = CheckMultiWordQuery(_creditBureauFilter, lowercaseQuery);
        
        // Check for match in Account Number column
        result["AccountNumber"] = CheckMultiWordQuery(_accountNumberFilter, lowercaseQuery);
        
        // Check for match in SSN column
        result["SSN"] = CheckMultiWordQuery(_ssnFilter, lowercaseQuery);
        
        // Check for match in Disputed Item Description column
        result["DisputedItemDescription"] = CheckMultiWordQuery(_disputedItemFilter, lowercaseQuery);
        
        // Check for match in Dispute Reason column
        result["DisputeReason"] = CheckMultiWordQuery(_disputeReasonFilter, lowercaseQuery);
        
        // Check for match in Account Status Before Dispute column
        result["AccountStatusBeforeDispute"] = CheckMultiWordQuery(_statusBeforeFilter, lowercaseQuery);
        
        // Check for match in Account Status After Dispute column
        result["AccountStatusAfterDispute"] = CheckMultiWordQuery(_statusAfterFilter, lowercaseQuery);
        
        // Check for match in Dispute Date column
        result["DisputeDate"] = CheckMultiWordQuery(_disputeDateFilter, lowercaseQuery);
        
        return result;
    }
}