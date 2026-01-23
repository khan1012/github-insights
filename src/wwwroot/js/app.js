// ==========================================
// GitHub Insights Dashboard - Main Application
// ==========================================

let detailsLoaded = false;

// Helper to escape HTML for safe display
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Fetch quick count first for fast page load
async function fetchQuickStats() {
    const errorEl = document.getElementById('error');
    const quickStatsEl = document.getElementById('quickStats');
    const refreshBtn = document.getElementById('refreshBtn');

    errorEl.style.display = 'none';
    if (refreshBtn) refreshBtn.disabled = true;

    try {
        console.log('Fetching quick stats...');
        const response = await fetch('/api/github/repos/count');
        
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({ message: `HTTP ${response.status}: ${response.statusText}` }));
            console.error('API Error:', errorData);
            throw new Error(errorData.message || errorData.error || 'Failed to fetch data');
        }

        const data = await response.json();
        console.log('Quick stats received:', data);

        // Update quick stats
        document.getElementById('repoCount').textContent = data.totalRepositories;
        document.getElementById('orgName').textContent = data.organization;
        
        const timestamp = new Date(data.timestamp).toLocaleString();
        document.getElementById('timestamp').textContent = `Last updated: ${timestamp}`;

        quickStatsEl.style.display = 'block';
        if (refreshBtn) refreshBtn.disabled = false;

        // Now fetch basic stats (fast) then contributors (slower)
        fetchBasicStats();

    } catch (error) {
        console.error('Error fetching quick stats:', error);
        // Preserve line breaks in error messages
        errorEl.innerHTML = `<strong>Error:</strong><br><pre style="white-space: pre-wrap; font-family: inherit; margin: 10px 0 0 0;">${escapeHtml(error.message)}</pre>`;
        errorEl.style.display = 'block';
        quickStatsEl.style.display = 'block'; // Show the container anyway
        if (refreshBtn) refreshBtn.disabled = false;
    }
}

// Fetch basic stats (without contributors) - renders quickly
async function fetchBasicStats() {
    if (detailsLoaded) return;

    const loadingDetailsEl = document.getElementById('loadingDetails');
    const detailedStatsEl = document.getElementById('detailedStats');

    loadingDetailsEl.style.display = 'block';
    loadingDetailsEl.innerHTML = '<p class="loading-text">Loading repository statistics<span class="loading-dots"><span>.</span><span>.</span><span>.</span></span></p>';

    try {
        const response = await fetch('/api/github/repos/basic');
        
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || errorData.error || 'Failed to fetch basic data');
        }

        const data = await response.json();

        // Update basic stats immediately
        document.getElementById('totalStars').textContent = data.totalStars.toLocaleString();
        document.getElementById('totalForks').textContent = data.totalForks.toLocaleString();
        document.getElementById('totalWatchers').textContent = data.totalWatchers.toLocaleString();
        document.getElementById('totalOpenIssues').textContent = data.totalOpenIssues.toLocaleString();
        document.getElementById('totalOpenPRs').textContent = data.totalOpenPullRequests.toLocaleString();
        document.getElementById('totalClosedPRs').textContent = data.totalClosedPullRequests.toLocaleString();
        
        // Show placeholders for contributor data
        document.getElementById('totalInternalContributors').innerHTML = '<span class="loading-dots"><span>.</span><span>.</span><span>.</span></span>';
        document.getElementById('totalExternalContributors').innerHTML = '<span class="loading-dots"><span>.</span><span>.</span><span>.</span></span>';

        // Show the details section immediately
        detailedStatsEl.style.display = 'block';
        loadingDetailsEl.innerHTML = '<p class="loading-text">Loading contributor statistics<span class="loading-dots"><span>.</span><span>.</span><span>.</span></span></p>';

        // Now fetch contributor stats in the background
        fetchContributorStats();

    } catch (error) {
        console.error('Error fetching basic stats:', error);
        loadingDetailsEl.style.display = 'none';
        // Don't show error if quick stats succeeded - just log it
        console.warn('Basic stats unavailable, but quick stats are shown');
    }
}

// Fetch contributor stats (slower operation)
async function fetchContributorStats() {
    const loadingDetailsEl = document.getElementById('loadingDetails');
    
    try {
        const response = await fetch('/api/github/repos/contributors');
        
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || errorData.error || 'Failed to fetch contributor data');
        }

        const data = await response.json();

        // Update contributor stats with smooth animation
        const internalEl = document.getElementById('totalInternalContributors');
        const externalEl = document.getElementById('totalExternalContributors');
        
        internalEl.classList.add('updating');
        externalEl.classList.add('updating');
        
        setTimeout(() => {
            internalEl.textContent = data.totalInternalContributors.toLocaleString();
            externalEl.textContent = data.totalExternalContributors.toLocaleString();
            internalEl.classList.remove('updating');
            externalEl.classList.remove('updating');
        }, 100);

        loadingDetailsEl.style.display = 'none';
        detailsLoaded = true;

        // Now fetch follower reach, dependent repositories, top contributors, and detailed insights (even slower operations)
        fetchFollowerReach();
        fetchDependentRepositories();
        fetchTopContributors();
        fetchDetailedInsights(data.totalInternalContributors, data.totalExternalContributors);

    } catch (error) {
        console.error('Error fetching contributor stats:', error);
        // Show error state for contributor fields
        document.getElementById('totalInternalContributors').textContent = 'N/A';
        document.getElementById('totalExternalContributors').textContent = 'N/A';
        loadingDetailsEl.style.display = 'none';
        detailsLoaded = true;
        
        // Still try to fetch follower reach, dependents, top contributors, and detailed insights
        fetchFollowerReach();
        fetchDependentRepositories();
        fetchTopContributors();
        fetchDetailedInsights(0, 0);
    }
}

// Fetch follower reach (slowest operation)
async function fetchFollowerReach() {
    const followerReachEl = document.getElementById('followerReach');
    const loadingDetailsEl = document.getElementById('loadingDetails');
    
    // Show loading state
    loadingDetailsEl.style.display = 'block';
    loadingDetailsEl.innerHTML = '<p class="loading-text">Calculating reach<span class="loading-dots"><span>.</span><span>.</span><span>.</span></span></p>';
    followerReachEl.innerHTML = '<span class="loading-dots"><span>.</span><span>.</span><span>.</span></span>';
    
    try {
        const response = await fetch('/api/github/repos/followerreach');
        
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || errorData.error || 'Failed to fetch follower reach');
        }

        const data = await response.json();

        // Update follower reach with smooth animation
        followerReachEl.classList.add('updating');
        
        setTimeout(() => {
            followerReachEl.textContent = data.totalFollowers.toLocaleString();
            followerReachEl.classList.remove('updating');
            loadingDetailsEl.style.display = 'none';
            
            // Optionally show tooltip with details
            followerReachEl.title = `Analyzed ${data.contributorsAnalyzed} contributors (${data.contributorsFailed} failed)`;
        }, 100);

    } catch (error) {
        console.error('Error fetching follower reach:', error);
        loadingDetailsEl.style.display = 'none';
        followerReachEl.textContent = 'N/A';
        followerReachEl.title = 'Failed to calculate follower reach';
    }
}

// Fetch dependent repositories
async function fetchDependentRepositories() {
    const dependentReposEl = document.getElementById('dependentRepos');
    const dependentDetailsEl = document.getElementById('dependentDetails');
    const topDependentsSectionEl = document.getElementById('topDependentsSection');
    
    // Show loading state
    dependentReposEl.innerHTML = '<span class="loading-dots"><span>.</span><span>.</span><span>.</span></span>';
    dependentDetailsEl.textContent = '';
    
    // Show loading message for top packages
    displayTopDependents(null);
    
    try {
        const response = await fetch('/api/github/repos/dependents');
        
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || errorData.error || 'Failed to fetch dependents');
        }

        const data = await response.json();

        // Update dependent count with animation
        dependentReposEl.classList.add('updating');
        
        setTimeout(() => {
            dependentReposEl.textContent = data.totalDependents.toLocaleString();
            dependentReposEl.classList.remove('updating');
            
            // Show details
            dependentDetailsEl.textContent = `${data.packageRepositories} packages analyzed`;
            dependentReposEl.title = `Analyzed ${data.repositoriesAnalyzed} repositories`;
            
            // Show top packages if we have them
            if (data.topRepositories && data.topRepositories.length > 0) {
                displayTopDependents(data.topRepositories);
                topDependentsSectionEl.style.display = 'block';
            }
        }, 100);

    } catch (error) {
        console.error('Error fetching dependent repositories:', error);
        dependentReposEl.textContent = 'N/A';
        dependentDetailsEl.textContent = '';
        dependentReposEl.title = 'Failed to fetch dependent repositories';
    }
}

// Display top packages by usage
function displayTopDependents(topRepos) {
    const listEl = document.getElementById('topDependentsList');
    const cardEl = document.getElementById('topDependentsCard');
    
    if (!topRepos || topRepos.length === 0) {
        listEl.innerHTML = '<div style="text-align: center; padding: 2rem; opacity: 0.7;">Analyzing package usage<span class="loading-dots"><span>.</span><span>.</span><span>.</span></span></div>';
        return;
    }
    
    const html = topRepos.map((repo, index) => `
        <div class="dependent-item" style="padding: 0.75rem 0; border-bottom: 1px solid #e0e0e0;">
            <div style="display: flex; justify-content: space-between; align-items: center;">
                <div>
                    <strong style="font-size: 1rem; color: #333;">${index + 1}. ${repo.name}</strong>
                    ${repo.ecosystem ? `<span class="badge" style="margin-left: 0.5rem; font-size: 0.7rem; background: #667eea; color: white; padding: 3px 8px; border-radius: 10px;">${repo.ecosystem}</span>` : ''}
                </div>
                <span style="color: #4CAF50; font-weight: bold;">${repo.dependentCount.toLocaleString()} repos</span>
            </div>
        </div>
    `).join('');
    
    listEl.innerHTML = html;
}

// Fetch and display top contributors
async function fetchTopContributors() {
    const tableBody = document.getElementById('topContributorsBody');
    
    try {
        const response = await fetch('/api/github/repos/topcontributors');
        
        if (!response.ok) {
            throw new Error('Failed to fetch top contributors');
        }

        const data = await response.json();
        
        if (!data.contributors || data.contributors.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 2rem; opacity: 0.7;">No contributor data available</td></tr>';
            return;
        }

        const html = data.contributors.map((contributor, index) => `
            <tr>
                <td style="font-weight: bold; color: #667eea;">#${index + 1}</td>
                <td>
                    <div style="display: flex; align-items: center; gap: 10px;">
                        ${contributor.avatarUrl ? `<img src="${contributor.avatarUrl}" alt="${contributor.username}" style="width: 32px; height: 32px; border-radius: 50%; border: 2px solid #e0e0e0;">` : ''}
                        <a href="${contributor.profileUrl}" target="_blank" style="color: #333; text-decoration: none; font-weight: 600;">
                            ${contributor.username}
                        </a>
                    </div>
                </td>
                <td>
                    <span style="padding: 4px 8px; border-radius: 12px; font-size: 0.85em; font-weight: 600; ${contributor.isInternal ? 'background: #e3f2fd; color: #1976d2;' : 'background: #f3e5f5; color: #7b1fa2;'}">
                        ${contributor.isInternal ? 'Internal' : 'External'}
                    </span>
                </td>
                <td style="font-weight: 600;">${contributor.totalContributions.toLocaleString()}</td>
                <td>${contributor.followers.toLocaleString()}</td>
                <td>${contributor.repositoriesContributedTo}</td>
            </tr>
        `).join('');
        
        tableBody.innerHTML = html;
        
    } catch (error) {
        console.error('Error fetching top contributors:', error);
        tableBody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 2rem; color: #999;">Failed to load contributors</td></tr>';
    }
}

// Display top contributors
function displayTopContributors(internalCount, externalCount) {
    const containerEl = document.getElementById('topContributors');
    const totalCount = internalCount + externalCount;
    
    const html = `
        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px;">
            <div style="text-align: center; padding: 1.5rem; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 10px; color: white;">
                <div style="font-size: 2.5em; font-weight: bold;">${totalCount.toLocaleString()}</div>
                <div style="font-size: 0.9em; opacity: 0.9; margin-top: 0.5rem;">Total Contributors</div>
            </div>
            <div style="text-align: center; padding: 1.5rem; background: #f8f9fa; border-radius: 10px; border: 2px solid #e0e0e0;">
                <div style="font-size: 2em; font-weight: bold; color: #667eea;">${internalCount.toLocaleString()}</div>
                <div style="font-size: 0.9em; color: #666; margin-top: 0.5rem;">🏢 Internal</div>
                <div style="font-size: 0.75em; color: #999; margin-top: 0.25rem;">Organization members</div>
            </div>
            <div style="text-align: center; padding: 1.5rem; background: #f8f9fa; border-radius: 10px; border: 2px solid #e0e0e0;">
                <div style="font-size: 2em; font-weight: bold; color: #4CAF50;">${externalCount.toLocaleString()}</div>
                <div style="font-size: 0.9em; color: #666; margin-top: 0.5rem;">🌍 External</div>
                <div style="font-size: 0.75em; color: #999; margin-top: 0.25rem;">Community contributors</div>
            </div>
        </div>
    `;
    
    containerEl.innerHTML = html;
}

// Refresh all data
async function refreshData() {
    detailsLoaded = false;
    document.getElementById('quickStats').style.display = 'none';
    document.getElementById('detailedStats').style.display = 'none';
    document.getElementById('detailedInsightsSection').style.display = 'none';
    await fetchQuickStats();
}

// Fetch and display detailed insights
async function fetchDetailedInsights(internalContributors = 0, externalContributors = 0) {
    const section = document.getElementById('detailedInsightsSection');
    
    try {
        const response = await fetch('/api/github/insights/detailed');
        
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || errorData.error || 'Failed to fetch detailed insights');
        }

        const data = await response.json();

        // Display top contributors
        if (internalContributors > 0 || externalContributors > 0) {
            displayTopContributors(internalContributors, externalContributors);
        }

        // Update repository health
        displayRepositoryHealth(data.health);

        // Update activity summary
        document.getElementById('totalEngagement').textContent = data.activity.totalEngagement.toLocaleString();
        document.getElementById('activeRepos').textContent = data.activity.activeRepositories.toLocaleString();
        document.getElementById('avgStars').textContent = data.activity.averageStarsPerRepo.toFixed(1);
        document.getElementById('avgForks').textContent = data.activity.averageForksPerRepo.toFixed(1);

        // Update top repositories table
        const tbody = document.getElementById('topReposBody');
        if (data.topRepositories && data.topRepositories.length > 0) {
            const rows = data.topRepositories.map((repo, index) => `
                <tr>
                    <td><strong>${index + 1}</strong></td>
                    <td>
                        <a href="${repo.url}" target="_blank" class="repo-link">${repo.name}</a>
                        ${repo.description ? `<div style="font-size: 0.85em; color: #666; margin-top: 3px;">${repo.description.substring(0, 80)}${repo.description.length > 80 ? '...' : ''}</div>` : ''}
                    </td>
                    <td>${repo.language ? `<span class="language-badge">${repo.language}</span>` : '-'}</td>
                    <td>${repo.stars.toLocaleString()}</td>
                    <td>${repo.forks.toLocaleString()}</td>
                    <td>${repo.openIssues.toLocaleString()}</td>
                    <td><span class="activity-score">${(repo.activityScore / 1000).toFixed(1)}k</span></td>
                </tr>
            `).join('');
            tbody.innerHTML = rows;
        } else {
            tbody.innerHTML = '<tr><td colspan="7" style="text-align: center; padding: 2rem; opacity: 0.7;">No repositories found</td></tr>';
        }

        // Update language distribution
        const langContainer = document.getElementById('languageDistribution');
        if (data.languageDistribution && data.languageDistribution.length > 0) {
            const langHtml = data.languageDistribution.slice(0, 8).map(lang => `
                <div class="language-bar-container">
                    <div class="language-bar-header">
                        <span class="language-name">${lang.language}</span>
                        <span class="language-count">${lang.repositoryCount} repos (${lang.percentage}%)</span>
                    </div>
                    <div class="language-bar" style="width: ${lang.percentage}%;">
                        ${lang.percentage}%
                    </div>
                </div>
            `).join('');
            langContainer.innerHTML = langHtml;
        } else {
            langContainer.innerHTML = '<p style="opacity: 0.7; text-align: center; padding: 1rem;">No language data available</p>';
        }

        // Show the section
        section.style.display = 'block';

    } catch (error) {
        console.error('Error fetching detailed insights:', error);
        // Don't show error - just log it
    }
}

// Display repository health overview
function displayRepositoryHealth(health) {
    // Update health counts
    document.getElementById('healthyCount').textContent = health.healthyCount.toLocaleString();
    document.getElementById('needsAttentionCount').textContent = health.needsAttentionCount.toLocaleString();
    document.getElementById('atRiskCount').textContent = health.atRiskCount.toLocaleString();
    document.getElementById('archivedCount').textContent = health.archivedCount.toLocaleString();
    document.getElementById('stalePercentage').textContent = `${health.stalePercentage}% stale`;

    // Show health details section if there are repos needing attention or at risk
    const hasHealthIssues = health.repositoriesNeedingAttention.length > 0 || health.atRiskRepositories.length > 0;
    const healthDetailsSection = document.getElementById('healthDetailsSection');
    
    if (hasHealthIssues) {
        healthDetailsSection.style.display = 'grid';
        
        // Display repos needing attention
        if (health.repositoriesNeedingAttention.length > 0) {
            const needsAttentionCard = document.getElementById('needsAttentionCard');
            const needsAttentionList = document.getElementById('needsAttentionList');
            
            const html = health.repositoriesNeedingAttention.map(repo => `
                <div class="health-repo-item" style="padding: 0.75rem; border-bottom: 1px solid #e0e0e0; display: flex; justify-content: space-between; align-items: center;">
                    <div>
                        <a href="${repo.url}" target="_blank" style="font-weight: 600; color: #333; text-decoration: none;">${repo.name}</a>
                        <div style="font-size: 0.85rem; color: #666; margin-top: 0.25rem;">${repo.reason}</div>
                    </div>
                    <div style="text-align: right; font-size: 0.85rem;">
                        <div style="color: #f39c12; font-weight: 600;">${repo.openIssues} issues</div>
                        <div style="color: #999; margin-top: 0.25rem;">${repo.daysSinceUpdate}d ago</div>
                    </div>
                </div>
            `).join('');
            
            needsAttentionList.innerHTML = html;
            needsAttentionCard.style.display = 'block';
        }
        
        // Display at-risk repos
        if (health.atRiskRepositories.length > 0) {
            const atRiskCard = document.getElementById('atRiskCard');
            const atRiskList = document.getElementById('atRiskList');
            
            const html = health.atRiskRepositories.map(repo => `
                <div class="health-repo-item" style="padding: 0.75rem; border-bottom: 1px solid #e0e0e0; display: flex; justify-content: space-between; align-items: center;">
                    <div>
                        <a href="${repo.url}" target="_blank" style="font-weight: 600; color: #333; text-decoration: none;">${repo.name}</a>
                        <div style="font-size: 0.85rem; color: #666; margin-top: 0.25rem;">${repo.reason}</div>
                    </div>
                    <div style="text-align: right; font-size: 0.85rem;">
                        <div style="color: #e74c3c; font-weight: 600;">${repo.openIssues} issues</div>
                        <div style="color: #999; margin-top: 0.25rem;">${repo.daysSinceUpdate}d ago</div>
                    </div>
                </div>
            `).join('');
            
            atRiskList.innerHTML = html;
            atRiskCard.style.display = 'block';
        }
    } else {
        healthDetailsSection.style.display = 'none';
    }
}

// Load quick stats on page load
fetchQuickStats();
