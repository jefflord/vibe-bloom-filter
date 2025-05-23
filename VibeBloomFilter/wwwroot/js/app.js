"use strict";
/**
 * Vibe Bloom Filter - Frontend Application
 *
 * This TypeScript application provides the frontend functionality for the Vibe Bloom Filter
 * demonstration, connecting to the backend API to query Bloom filters and display results.
 */
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
// API BASE URL for the backend
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
    
    // Hide the sample data section completely
    const sampleDataTable = document.getElementById('sampleDataTable');
    if (sampleDataTable) {
        sampleDataTable.style.display = 'none';
    }
    
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
    function performBloomFilterQuery(query) {
        return __awaiter(this, void 0, void 0, function* () {
            try {
                // Show loading state
                searchButton.disabled = true;
                searchButton.textContent = 'Searching...';
                
                // Send query to the backend API
                const response = yield fetch(`${API_BASE_URL}/query?query=${encodeURIComponent(query)}`);
                if (!response.ok) {
                    throw new Error(`Error: ${response.statusText}`);
                }
                
                // Parse response data
                const result = yield response.json();
                
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
        });
    }
    
    /**
     * Generates sample JSON files by calling the backend API
     */
    function generateSampleFiles() {
        return __awaiter(this, void 0, void 0, function* () {
            try {
                // Show loading state
                generateFilesButton.disabled = true;
                generateFilesButton.textContent = 'Generating...';
                generationStatus.textContent = 'Generating files...';
                generationStatus.className = 'status-message';
                
                // Call the backend API to generate files
                const response = yield fetch(`${API_BASE_URL}/generate`, {
                    method: 'POST'
                });
                if (!response.ok) {
                    throw new Error(`Error: ${response.statusText}`);
                }
                
                // Parse response data
                const result = yield response.json();
                
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
                generateFilesButton.textContent = 'Generate Sample JSON Files';
            }
        });
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
        
        // Update indicators for all new fields
        updateIndicator(document.querySelector('#creditBureauResult .indicator'), result.CreditBureau);
        updateIndicator(document.querySelector('#accountNumberResult .indicator'), result.AccountNumber);
        updateIndicator(document.querySelector('#ssnResult .indicator'), result.SSN);
        updateIndicator(document.querySelector('#disputedItemResult .indicator'), result.DisputedItemDescription);
        updateIndicator(document.querySelector('#disputeReasonResult .indicator'), result.DisputeReason);
        updateIndicator(document.querySelector('#statusBeforeResult .indicator'), result.AccountStatusBeforeDispute);
        updateIndicator(document.querySelector('#statusAfterResult .indicator'), result.AccountStatusAfterDispute);
        updateIndicator(document.querySelector('#disputeDateResult .indicator'), result.DisputeDate);
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
    
    // Note: loadSampleData function removed since we no longer need to display sample data
});