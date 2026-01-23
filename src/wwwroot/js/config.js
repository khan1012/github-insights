// ==========================================
// API Endpoints & Configuration
// ==========================================

const API_CONFIG = {
    endpoints: {
        repoCount: '/api/github/repos/count',
        repoDetails: '/api/github/repos/details'
    }
};

const UI_CONFIG = {
    elements: {
        repoCount: 'repo-count',
        totalStars: 'total-stars',
        totalForks: 'total-forks',
        openPRs: 'open-prs',
        openIssues: 'open-issues',
        topRepoCard: 'top-repo-card',
        errorMessage: 'error-message',
        refreshBtn: 'refresh-btn',
        lastUpdated: 'last-updated'
    },
    refreshCooldown: 2000
};
