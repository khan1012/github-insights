// ==========================================
// UI Update Functions
// Handles updating the UI with data
// ==========================================

const UIUpdater = {
    /**
     * Update quick stats (repo count)
     */
    updateQuickStats(count) {
        DOMHelper.updateText(UI_CONFIG.elements.repoCount, count);
        DOMHelper.removeLoadingState(UI_CONFIG.elements.repoCount);
    },

    /**
     * Update detailed statistics
     */
    updateDetailedStats(data) {
        // Update all stat values
        DOMHelper.updateText(UI_CONFIG.elements.totalStars, data.totalStars);
        DOMHelper.updateText(UI_CONFIG.elements.totalForks, data.totalForks);
        DOMHelper.updateText(UI_CONFIG.elements.openPRs, data.openPRs);
        DOMHelper.updateText(UI_CONFIG.elements.openIssues, data.openIssues);

        // Remove loading states
        DOMHelper.removeLoadingState(UI_CONFIG.elements.totalStars);
        DOMHelper.removeLoadingState(UI_CONFIG.elements.totalForks);
        DOMHelper.removeLoadingState(UI_CONFIG.elements.openPRs);
        DOMHelper.removeLoadingState(UI_CONFIG.elements.openIssues);

        // Update top repository if available
        if (data.topRepository) {
            this.updateTopRepository(data.topRepository);
        }

        // Update timestamp
        this.updateTimestamp();
    },

    /**
     * Update top repository card
     */
    updateTopRepository(repo) {
        const html = `
            <div class="top-repo-title">🏆 Top Repository</div>
            <a href="${repo.url}" target="_blank" class="repo-name">${repo.name}</a>
            ${repo.description ? `<p class="repo-description">${repo.description}</p>` : ''}
            <div class="repo-stats">
                ⭐ ${repo.stargazersCount} stars • 
                🍴 ${repo.forksCount} forks • 
                👀 ${repo.watchersCount} watchers
            </div>
        `;
        
        DOMHelper.setHTML(UI_CONFIG.elements.topRepoCard, html);
        DOMHelper.show(UI_CONFIG.elements.topRepoCard);
        DOMHelper.addClass(UI_CONFIG.elements.topRepoCard, 'fade-in');
    },

    /**
     * Show loading state for quick stats
     */
    showQuickStatsLoading() {
        DOMHelper.updateText(UI_CONFIG.elements.repoCount, 'Loading...');
        DOMHelper.addLoadingState(UI_CONFIG.elements.repoCount);
    },

    /**
     * Show loading state for detailed stats
     */
    showDetailedStatsLoading() {
        const detailStats = [
            UI_CONFIG.elements.totalStars,
            UI_CONFIG.elements.totalForks,
            UI_CONFIG.elements.openPRs,
            UI_CONFIG.elements.openIssues
        ];

        detailStats.forEach(id => {
            DOMHelper.updateText(id, 'Loading...');
            DOMHelper.addLoadingState(id);
        });
    },

    /**
     * Show error message
     */
    showError(error) {
        DOMHelper.updateText(UI_CONFIG.elements.errorMessage, 'Error: ' + error.message);
        DOMHelper.show(UI_CONFIG.elements.errorMessage);
    },

    /**
     * Hide error message
     */
    hideError() {
        DOMHelper.hide(UI_CONFIG.elements.errorMessage);
    },

    /**
     * Update last updated timestamp
     */
    updateTimestamp() {
        const timestamp = 'Last updated: ' + new Date().toLocaleString();
        DOMHelper.updateText(UI_CONFIG.elements.lastUpdated, timestamp);
    }
};
