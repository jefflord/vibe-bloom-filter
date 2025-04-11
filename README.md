# Vibe Bloom Filter

A web application that demonstrates the use of Bloom filters for efficient data lookup. This application was built with .NET 9 backend and TypeScript frontend.

## About Bloom Filters

Bloom filters are space-efficient probabilistic data structures designed to test whether an element is a member of a set. False positive matches are possible, but false negatives are not – in other words, a query returns either "possibly in set" or "definitely not in set".

The implementation in this application uses a false positive rate of 1%, which was chosen as a good balance between accuracy and memory usage. Lower rates would significantly increase memory usage, while higher rates would result in too many false positives, reducing the filter's utility.

## Project Structure

- **Backend (.NET 9)**
  - Minimal API architecture
  - Sample data generation (100 rows with Name, Address, and UserId columns)
  - Bloom filter implementation for each data column
  - API endpoints for querying bloom filters and retrieving sample data

- **Frontend (TypeScript)**
  - Responsive user interface
  - Search functionality to query the backend Bloom filters
  - Display of search results and sample data
  - Clear visual indicators for query results

## Implementation Details

### Bloom Filter Parameters
- **Expected Element Count**: 100 (matches the number of rows in our sample data)
- **False Positive Rate**: 1%
- **Hash Function**: Murmur3 (provided by the BloomFilter.NetCore library)

These parameters were chosen to balance memory efficiency with accuracy. With a 1% false positive rate, the Bloom filters provide high confidence in negative results (100% certainty) while maintaining reasonable confidence in positive results (99% certainty).

### Future Database Considerations
While this example uses an in-memory DataTable, the implementation is designed with Microsoft SQL Server in mind for future enhancements. The Bloom filter approach can significantly reduce the need for expensive database queries by quickly determining if a value might exist in a specific column.

## Setup Instructions

### Prerequisites
- .NET 9 SDK
- Node.js and npm (for TypeScript compilation)
- TypeScript compiler (install globally with `npm install -g typescript`)

### Running the Application

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/vibe-bloom-filter.git
   cd vibe-bloom-filter
   ```

2. Build and run the .NET application:
   ```
   cd VibeBloomFilter
   dotnet build
   dotnet run
   ```

3. Access the application in your browser:
   ```
   http://localhost:5113
   ```

### Development Workflow

For frontend changes:
1. Edit TypeScript files in `wwwroot/ts/`
2. Compile with `tsc` from the `wwwroot` directory
3. Refresh your browser to see changes

For backend changes:
1. Edit C# files
2. Restart the application with `dotnet run`

## API Documentation

### GET /api/bloomfilter/query
Query the Bloom filters to see if a value might exist in any column.

**Parameters:**
- `query` (string): The value to search for

**Response:**
```json
{
  "Name": true,
  "Address": false,
  "UserId": true
}
```

### GET /api/bloomfilter/sample
Get sample data from the in-memory DataTable.

**Parameters:**
- `count` (int, optional): Number of rows to return. Default: 10

**Response:**
```json
[
  {
    "Name": "John Smith",
    "Address": "123 Main St, New York",
    "UserId": 10001
  },
  ...
]
```