// ==========================================
// API Service Layer
// Handles all backend communication
// ==========================================

const ApiService = {
    /**
     * Fetch repository count (quick stats)
     */
    async fetchRepoCount() {
        const response = await fetch(API_CONFIG.endpoints.repoCount);
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        return await response.json();
    },

    /**
     * Fetch detailed repository statistics
     */
    async fetchRepoDetails() {
        const response = await fetch(API_CONFIG.endpoints.repoDetails);
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        return await response.json();
    }
};
