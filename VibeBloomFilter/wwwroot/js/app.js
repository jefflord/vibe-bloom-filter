"use strict";
/**
 * Vibe Bloom Filter - Frontend Application
 *
 * This TypeScript application provides the frontend functionality for the Vibe Bloom Filter
 * demonstration, connecting to the backend API to query Bloom filters and display results.
 */
// API URL for the backend
const API_BASE_URL = '/api/bloomfilter';
// Wait for the DOM to be fully loaded before attaching events
document.addEventListener('DOMContentLoaded', () => {
    // Get references to DOM elements
    const searchInput = document.getElementById('searchInput');
    const searchButton = document.getElementById('searchButton');
    const searchResults = document.getElementById('searchResults');
    const nameResult = document.getElementById('nameResult');
    const addressResult = document.getElementById('addressResult');
    const userIdResult = document.getElementById('userIdResult');
    const generateFilesButton = document.getElementById('generateFilesButton');
    const generationStatus = document.getElementById('generationStatus');
    const sampleDataTableBody = document.querySelector('#sampleDataTable tbody');
    // Load sample data when the page loads
    loadSampleData();
    // Add click event listener to the search button
    searchButton.addEventListener('click', () => {
        const query = searchInput.value.trim();
        if (query) {
            performBloomFilterQuery(query);
        }
        else {
            alert('Please enter a value to search for');
        }
    });
    // Add enter key event listener to the search input
    searchInput.addEventListener('keyup', (event) => {
        if (event.key === 'Enter') {
            searchButton.click();
        }
    });
    // Add click event listener to the generate files button
    generateFilesButton.addEventListener('click', generateSampleFiles);
    /**
     * Queries the backend API with the provided search term and updates the UI with results
     * @param query The search term to query
     */
    async function performBloomFilterQuery(query) {
        try {
            // Show loading state
            searchButton.disabled = true;
            searchButton.textContent = 'Searching...';
            // Send query to the backend API
            const response = await fetch(`${API_BASE_URL}/query?query=${encodeURIComponent(query)}`);
            if (!response.ok) {
                throw new Error(`Error: ${response.statusText}`);
            }
            // Parse response data
            const result = await response.json();
            // Update the UI with results
            updateResultDisplay(result);
            // Show the results section
            searchResults.classList.remove('hidden');
        }
        catch (error) {
            console.error('Error querying bloom filters:', error);
            alert('An error occurred while querying the bloom filters. Please try again.');
        }
        finally {
            // Restore button state
            searchButton.disabled = false;
            searchButton.textContent = 'Search';
        }
    }
    /**
     * Generates sample JSON files by calling the backend API
     */
    async function generateSampleFiles() {
        try {
            // Show loading state
            generateFilesButton.disabled = true;
            generateFilesButton.textContent = 'Generating...';
            generationStatus.textContent = 'Generating files...';
            generationStatus.className = 'status-message';
            // Call the backend API to generate files
            const response = await fetch(`${API_BASE_URL}/generate`, {
                method: 'POST'
            });
            if (!response.ok) {
                throw new Error(`Error: ${response.statusText}`);
            }
            // Parse response data
            const result = await response.json();
            // Update UI with success message
            generationStatus.textContent = `${result.message}`;
            generationStatus.className = 'status-message success';
            // Show the generated files for 5 seconds
            setTimeout(() => {
                generationStatus.textContent = '';
                generationStatus.className = 'status-message';
            }, 5000);
        }
        catch (error) {
            console.error('Error generating sample files:', error);
            const errorMessage = error instanceof Error ? error.message : 'Unknown error';
            generationStatus.textContent = `Error: ${errorMessage}`;
            generationStatus.className = 'status-message error';
        }
        finally {
            // Restore button state
            generateFilesButton.disabled = false;
            generateFilesButton.textContent = 'Generate 10 Sample JSON Files';
        }
    }
    /**
     * Updates the UI to display bloom filter query results
     * @param result The bloom filter query results
     */
    function updateResultDisplay(result) {
        // Update the indicator for each column
        updateIndicator(nameResult.querySelector('.indicator'), result.Name);
        updateIndicator(addressResult.querySelector('.indicator'), result.Address);
        updateIndicator(userIdResult.querySelector('.indicator'), result.UserId);
    }
    /**
     * Updates a specific indicator element based on the result value
     * @param element The indicator DOM element to update
     * @param isPresent Whether the value is potentially present in the filter
     */
    function updateIndicator(element, isPresent) {
        // Clear existing classes
        element.classList.remove('positive', 'negative');
        // Set appropriate class and text based on result
        if (isPresent) {
            element.classList.add('positive');
            element.textContent = 'Yes';
        }
        else {
            element.classList.add('negative');
            element.textContent = 'No';
        }
    }
    /**
     * Loads sample data from the backend API and displays it in the table
     */
    async function loadSampleData() {
        try {
            // Request sample data from the API
            const response = await fetch(`${API_BASE_URL}/sample?count=10`);
            if (!response.ok) {
                throw new Error(`Error: ${response.statusText}`);
            }
            // Parse response data
            const sampleData = await response.json();
            // Clear existing rows
            sampleDataTableBody.innerHTML = '';
            // Populate table with sample data
            sampleData.forEach(item => {
                const row = document.createElement('tr');
                row.innerHTML = `
                    <td>${escapeHtml(item.Name)}</td>
                    <td>${escapeHtml(item.Address)}</td>
                    <td>${item.UserId}</td>
                `;
                sampleDataTableBody.appendChild(row);
            });
        }
        catch (error) {
            console.error('Error loading sample data:', error);
            sampleDataTableBody.innerHTML = `
                <tr>
                    <td colspan="3" class="error-message">Failed to load sample data. Please refresh the page to try again.</td>
                </tr>
            `;
        }
    }
    /**
     * Helper function to escape HTML special characters to prevent XSS
     * @param text The text to escape
     * @returns Escaped text safe for insertion into HTML
     */
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
});
