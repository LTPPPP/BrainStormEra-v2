/**
 * Points Updater - Automatically updates user points every 5 minutes
 */
class PointsUpdater {
    constructor() {
        this.updateInterval = 5 * 60 * 1000; // 5 minutes in milliseconds
        this.pointsElement = null;
        this.isAuthenticated = false;
        this.userId = null;
        this.intervalId = null;
        
        this.init();
    }

    init() {
        // Check if user is authenticated
        this.isAuthenticated = document.querySelector('.user-avatar') !== null;
        
        if (!this.isAuthenticated) {
            console.log('PointsUpdater: User not authenticated, skipping initialization');
            return;
        }

        // Find points display element
        this.pointsElement = document.getElementById('userPoints');
        if (!this.pointsElement) {
            console.warn('PointsUpdater: Points element not found');
            return;
        }

        // Extract user ID from claims (if available in page)
        this.userId = this.extractUserId();
        
        console.log('PointsUpdater: Initialized for authenticated user');
        
        // Start periodic updates
        this.startPeriodicUpdate();
        
        // Also update on page visibility change (when user returns to tab)
        document.addEventListener('visibilitychange', () => {
            if (!document.hidden) {
                this.updatePoints();
            }
        });
    }

    extractUserId() {
        // Try to get user ID from various sources
        const userIdElement = document.querySelector('[data-user-id]');
        if (userIdElement) {
            return userIdElement.dataset.userId;
        }

        // Check if user ID is in a global variable
        if (typeof window.currentUserId !== 'undefined') {
            return window.currentUserId;
        }

        // Try to extract from avatar URL
        const avatarElement = document.querySelector('.user-avatar img');
        if (avatarElement && avatarElement.src) {
            const match = avatarElement.src.match(/userId=([^&]+)/);
            if (match) {
                return match[1];
            }
        }

        return null;
    }

    startPeriodicUpdate() {
        // Update immediately on page load
        this.updatePoints();
        
        // Set up periodic updates
        this.intervalId = setInterval(() => {
            this.updatePoints();
        }, this.updateInterval);
        
        console.log(`PointsUpdater: Started periodic updates every ${this.updateInterval / 1000} seconds`);
    }

    async updatePoints() {
        try {
            console.log('PointsUpdater: Updating points...');
            
            const response = await fetch('/api/points/current', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                credentials: 'same-origin'
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            
            if (data.success) {
                this.updatePointsDisplay(data.points);
                console.log(`PointsUpdater: Points updated to ${data.points}`);
            } else {
                console.warn('PointsUpdater: Failed to get points:', data.message);
            }
        } catch (error) {
            console.error('PointsUpdater: Error updating points:', error);
        }
    }

    async refreshPointsClaim() {
        try {
            console.log('PointsUpdater: Refreshing points claim...');
            
            const response = await fetch('/api/points/refresh', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                credentials: 'same-origin'
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            
            if (data.success) {
                this.updatePointsDisplay(data.points);
                console.log(`PointsUpdater: Points claim refreshed to ${data.points}`);
                return true;
            } else {
                console.warn('PointsUpdater: Failed to refresh points claim:', data.message);
                return false;
            }
        } catch (error) {
            console.error('PointsUpdater: Error refreshing points claim:', error);
            return false;
        }
    }

    updatePointsDisplay(points) {
        if (this.pointsElement) {
            // Format points with comma separators
            const formattedPoints = new Intl.NumberFormat().format(points);
            
            // Add updating animation class
            this.pointsElement.classList.add('updating');
            
            // Update the text
            this.pointsElement.textContent = formattedPoints;
            
            // Remove animation class after animation completes
            setTimeout(() => {
                this.pointsElement.classList.remove('updating');
            }, 500);
        }
    }

    stop() {
        if (this.intervalId) {
            clearInterval(this.intervalId);
            this.intervalId = null;
            console.log('PointsUpdater: Stopped periodic updates');
        }
    }

    // Public method to force update
    forceUpdate() {
        this.updatePoints();
    }

    // Public method to force refresh claim
    async forceRefreshClaim() {
        return await this.refreshPointsClaim();
    }
}

// Initialize points updater when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.pointsUpdater = new PointsUpdater();
});

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PointsUpdater;
} 