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
    
    // Logger
    private readonly ILogger<BloomFilterService> _logger;
    
    // Flag to indicate if data was loaded from JSON files
    public bool LoadedFromJsonFiles { get; private set; }

    // Statistics for bloom filter population
    public int TotalRecordsProcessed { get; private set; }
    public int TotalFieldsAdded { get; private set; }

    // False positive rate for all filters (1%)
    // This rate was chosen as a good balance between accuracy and memory usage.
    // Lower rates would increase memory usage significantly while higher rates
    // would lead to too many false positives, reducing the utility of the filters.
    private const double FalsePositiveRate = 0.01;
    
    // Expected element count (3 million items)
    private const int ExpectedElementCount = 3000000;

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
        
        // Populate bloom filters with data - now without keeping the data in memory
        _logger.LogInformation("Populating bloom filters from JSON files");
        LoadedFromJsonFiles = PopulateBloomFiltersFromJsonFiles();
        
        // If no data was found in JSON files, use generate sample data instead
        if (!LoadedFromJsonFiles)
        {
            _logger.LogInformation("No data found in JSON files, generating sample data");
            PopulateBloomFiltersFromGeneratedData();
        }
        
        // Log the statistics after building the bloom filter
        _logger.LogInformation("Bloom Filter Statistics: {TotalRecords} records processed, {TotalFields} fields added to filters", 
            TotalRecordsProcessed, TotalFieldsAdded);
        
        // Console output for better visibility
        Console.WriteLine($"=== Bloom Filter Build Complete ===");
        Console.WriteLine($"Total Records Processed: {TotalRecordsProcessed:N0}");
        Console.WriteLine($"Total Fields Added: {TotalFieldsAdded:N0}");
        Console.WriteLine($"===============================");
        
        // Force garbage collection to clean up any memory used during loading
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// Populates bloom filters directly from JSON files without keeping all records in memory
    /// </summary>
    /// <returns>True if data was found and loaded, false otherwise</returns>
    private bool PopulateBloomFiltersFromJsonFiles()
    {
        try
        {
            // Define common stop words to exclude from the bloom filter
            HashSet<string> stopWords = GetStopWords();
            
            // Get the Data directory path
            var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            _logger.LogInformation("Looking for JSON files in directory: {Directory}", dataDirectory);
            
            // If the directory doesn't exist, return false (will use generated data)
            if (!Directory.Exists(dataDirectory))
            {
                _logger.LogWarning("Data directory does not exist: {Directory}", dataDirectory);
                return false;
            }
            
            // Get all JSON files in the Data directory
            var jsonFiles = Directory.GetFiles(dataDirectory, "*.json");
            _logger.LogInformation("Found {Count} JSON files in the Data directory", jsonFiles.Length);
            
            // If no files are found, return false
            if (jsonFiles.Length == 0)
            {
                _logger.LogWarning("No JSON files found in Data directory");
                return false;
            }
            
            // Process each JSON file
            int totalRowsProcessed = 0;
            foreach (var file in jsonFiles)
            {
                try
                {
                    _logger.LogDebug("Reading file: {FileName}", Path.GetFileName(file));
                    
                    // Process the file in a streaming manner
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    using (JsonDocument doc = JsonDocument.Parse(fs))
                    {
                        int rowsProcessed = 0;
                        JsonElement root = doc.RootElement;
                        
                        // Process each JSON object in the array
                        foreach (JsonElement item in root.EnumerateArray())
                        {
                            ProcessJsonItem(item, stopWords);
                            rowsProcessed++;
                        }
                        
                        _logger.LogInformation("Processed {RowCount} rows from file {FileName}", rowsProcessed, Path.GetFileName(file));
                        totalRowsProcessed += rowsProcessed;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading JSON file {FileName}", Path.GetFileName(file));
                    // Continue to the next file if one fails
                }
            }
            
            _logger.LogInformation("Total rows processed from all JSON files: {TotalRowCount}", totalRowsProcessed);
            TotalRecordsProcessed = totalRowsProcessed;
            return totalRowsProcessed > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading JSON files");
            return false;
        }
    }
    
    /// <summary>
    /// Processes a single JSON object and adds its values to bloom filters
    /// </summary>
    private void ProcessJsonItem(JsonElement item, HashSet<string> stopWords)
    {
        try
        {
            // Extract values from the JSON item
            string name = GetJsonString(item, "Name");
            string address = GetJsonString(item, "Address");
            string creditBureau = GetJsonString(item, "CreditBureau");
            string accountNumber = GetJsonString(item, "AccountNumber");
            string ssn = GetJsonString(item, "SSN");
            string disputedItemDescription = GetJsonString(item, "DisputedItemDescription");
            string disputeReason = GetJsonString(item, "DisputeReason");
            string accountStatusBeforeDispute = GetJsonString(item, "AccountStatusBeforeDispute");
            string accountStatusAfterDispute = GetJsonString(item, "AccountStatusAfterDispute");
            string disputeDate = GetJsonString(item, "DisputeDate");

            // Process UserId
            if (item.TryGetProperty("UserId", out JsonElement userIdElement) && 
                userIdElement.ValueKind == JsonValueKind.Number)
            {
                int userId = userIdElement.GetInt32();
                _userIdFilter.Add(BitConverter.GetBytes(userId));
                TotalFieldsAdded++;
            }
            
            // Add extracted strings to respective filters
            AddWordsToFilter(_nameFilter, name, stopWords);
            AddWordsToFilter(_addressFilter, address, stopWords);
            AddWordsToFilter(_creditBureauFilter, creditBureau, stopWords);
            AddWordsToFilter(_accountNumberFilter, accountNumber, stopWords);
            AddWordsToFilter(_ssnFilter, ssn, stopWords);
            AddWordsToFilter(_disputedItemFilter, disputedItemDescription, stopWords);
            AddWordsToFilter(_disputeReasonFilter, disputeReason, stopWords);
            AddWordsToFilter(_statusBeforeFilter, accountStatusBeforeDispute, stopWords);
            AddWordsToFilter(_statusAfterFilter, accountStatusAfterDispute, stopWords);
            AddWordsToFilter(_disputeDateFilter, disputeDate, stopWords);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing JSON item");
        }
    }
    
    /// <summary>
    /// Helper method to extract string from JsonElement
    /// </summary>
    private string GetJsonString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property))
        {
            return property.ToString();
        }
        return string.Empty;
    }

    /// <summary>
    /// Populates bloom filters with generated sample data
    /// </summary>
    private void PopulateBloomFiltersFromGeneratedData()
    {
        HashSet<string> stopWords = GetStopWords();
        
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
        
        // Generate 100 records one at a time and add to filters without keeping them in memory
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
            
            var statusBeforeIndex = random.Next(accountStatuses.Length);
            var accountStatusBeforeDispute = accountStatuses[statusBeforeIndex];
            var accountStatusAfterDispute = reviewStatuses[random.Next(reviewStatuses.Length)];
            
            // Add userId to filter
            _userIdFilter.Add(BitConverter.GetBytes(userId));
            TotalFieldsAdded++;
            
            // Add string values to filters
            AddWordsToFilter(_nameFilter, name, stopWords);
            AddWordsToFilter(_addressFilter, address, stopWords);
            AddWordsToFilter(_creditBureauFilter, creditBureau, stopWords);
            AddWordsToFilter(_accountNumberFilter, accountNumber, stopWords);
            AddWordsToFilter(_ssnFilter, ssn, stopWords);
            AddWordsToFilter(_disputedItemFilter, disputedItemDescription, stopWords);
            AddWordsToFilter(_disputeReasonFilter, disputeReason, stopWords);
            AddWordsToFilter(_statusBeforeFilter, accountStatusBeforeDispute, stopWords);
            AddWordsToFilter(_statusAfterFilter, accountStatusAfterDispute, stopWords);
            AddWordsToFilter(_disputeDateFilter, disputeDate, stopWords);
        }
        TotalRecordsProcessed = 100;
    }
    
    /// <summary>
    /// Returns a HashSet of common stop words to exclude from the bloom filter
    /// </summary>
    private HashSet<string> GetStopWords()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
    }

    /// <summary>
    /// Adds words to filter, handling stop words and individual word indexing
    /// </summary>
    private void AddWordsToFilter(IBloomFilter filter, string text, HashSet<string> stopWords)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        // Convert to lowercase for case-insensitive filtering
        string lowercaseText = text.ToLowerInvariant();
        
        // Remove punctuation from the text before adding to bloom filter
        string textWithoutPunctuation = RemovePunctuation(lowercaseText);
        
        // Add the full text without punctuation
        if (!string.IsNullOrWhiteSpace(textWithoutPunctuation))
        {
            filter.Add(Encoding.UTF8.GetBytes(textWithoutPunctuation));
            TotalFieldsAdded++;
        }

        // If text contains multiple words, add individual words separately
        string[] words = textWithoutPunctuation.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
        if (words.Length > 1)
        {
            foreach (string word in words)
            {
                // Skip stop words
                if (!stopWords.Contains(word) && word.Length > 1)
                {
                    filter.Add(Encoding.UTF8.GetBytes(word));
                    TotalFieldsAdded++;
                }
            }
        }
    }
    
    /// <summary>
    /// Helper method to remove punctuation from text
    /// </summary>
    private string RemovePunctuation(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        var sb = new StringBuilder();
        foreach (char c in text)
        {
            if (!char.IsPunctuation(c))
            {
                sb.Append(c);
            }
            else if (c == ' ' || c == '-') // Keep spaces and hyphens as they may be important for certain terms
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

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
        HashSet<string> stopWords = GetStopWords();
        
        // Convert query to lowercase for case-insensitive search
        string lowercaseQuery = query.ToLowerInvariant();
        
        // Remove punctuation from the query
        string queryWithoutPunctuation = RemovePunctuation(lowercaseQuery);
        
        // Helper function to check if any word from a multi-word query matches in a filter
        bool CheckMultiWordQuery(IBloomFilter filter, string queryText)
        {
            // First, check if the full phrase matches
            byte[] fullQueryBytes = Encoding.UTF8.GetBytes(queryText);
            bool fullMatch = filter.Contains(fullQueryBytes);
            
            if (fullMatch)
                return true;
                
            // Split into words and check each non-stop word
            string[] words = queryText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
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
        result["Name"] = CheckMultiWordQuery(_nameFilter, queryWithoutPunctuation);
        
        // Check for match in Address column
        result["Address"] = CheckMultiWordQuery(_addressFilter, queryWithoutPunctuation);
        
        // Check for match in UserId column (if query can be parsed as int)
        bool userIdMatch = false;
        if (int.TryParse(query, out int userId))
        {
            byte[] userIdBytes = BitConverter.GetBytes(userId);
            userIdMatch = _userIdFilter.Contains(userIdBytes);
        }
        result["UserId"] = userIdMatch;
        
        // Check for match in Credit Bureau column
        result["CreditBureau"] = CheckMultiWordQuery(_creditBureauFilter, queryWithoutPunctuation);
        
        // Check for match in Account Number column
        result["AccountNumber"] = CheckMultiWordQuery(_accountNumberFilter, queryWithoutPunctuation);
        
        // Check for match in SSN column
        result["SSN"] = CheckMultiWordQuery(_ssnFilter, queryWithoutPunctuation);
        
        // Check for match in Disputed Item Description column
        result["DisputedItemDescription"] = CheckMultiWordQuery(_disputedItemFilter, queryWithoutPunctuation);
        
        // Check for match in Dispute Reason column
        result["DisputeReason"] = CheckMultiWordQuery(_disputeReasonFilter, queryWithoutPunctuation);
        
        // Check for match in Account Status Before Dispute column
        result["AccountStatusBeforeDispute"] = CheckMultiWordQuery(_statusBeforeFilter, queryWithoutPunctuation);
        
        // Check for match in Account Status After Dispute column
        result["AccountStatusAfterDispute"] = CheckMultiWordQuery(_statusAfterFilter, queryWithoutPunctuation);
        
        // Check for match in Dispute Date column
        result["DisputeDate"] = CheckMultiWordQuery(_disputeDateFilter, queryWithoutPunctuation);
        
        return result;
    }
}