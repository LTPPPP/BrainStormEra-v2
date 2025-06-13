/**
 * Admin Dashboard Charts JavaScript
 * Handles chart initialization and data visualization
 */

class AdminDashboardCharts {
    constructor(adminData) {
        this.adminData = adminData;
        this.charts = {};
        
        // Professional color palette
        this.colors = {
            primary: '#2c3e50',
            secondary: '#34495e',
            success: '#27ae60',
            warning: '#f39c12',
            danger: '#e74c3c',
            info: '#2980b9',
            light: '#ecf0f1',
            dark: '#2c3e50',
            blue: '#3498db',
            indigo: '#6c5ce7',
            purple: '#9b59b6',
            pink: '#e91e63',
            red: '#e74c3c',
            orange: '#fd7e14',
            yellow: '#f1c40f',
            green: '#27ae60',
            teal: '#17a2b8',
            cyan: '#6f42c1'
        };
        
        this.init();
    }

    init() {
        // Set Chart.js global configuration
        this.setupChartDefaults();
        
        // Initialize all charts
        this.initializeCharts();
        
        // Initialize AOS animations
        this.initializeAnimations();
    }

    setupChartDefaults() {
        Chart.defaults.font.family = "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif";
        Chart.defaults.font.size = 12;
        Chart.defaults.color = '#495057';
    }

    // Extract real data from server
    getUserGrowthData() {
        if (this.adminData.userGrowthData && this.adminData.userGrowthData.length > 0) {
            return {
                labels: this.adminData.userGrowthData.map(data => data.month),
                data: this.adminData.userGrowthData.map(data => data.newUsers)
            };
        }
        // Fallback to sample data if no real data
        const months = ['January', 'February', 'March', 'April', 'May', 'June'];
        return {
            labels: months,
            data: [1, 1, 1, 1, 1, 1]
        };
    }

    getRevenueData() {
        if (this.adminData.revenueData && this.adminData.revenueData.length > 0) {
            return {
                labels: this.adminData.revenueData.map(data => data.month),
                data: this.adminData.revenueData.map(data => Math.round(data.revenue))
            };
        }
        // Fallback to sample data if no real data
        const months = ['January', 'February', 'March', 'April', 'May', 'June'];
        return {
            labels: months,
            data: [50, 75, 100, 125, 150, 175]
        };
    }

    getEnrollmentData() {
        if (this.adminData.enrollmentData && this.adminData.enrollmentData.length > 0) {
            return {
                labels: this.adminData.enrollmentData.map(data => data.week),
                enrollments: this.adminData.enrollmentData.map(data => data.newEnrollments),
                completions: this.adminData.enrollmentData.map(data => data.completedCourses)
            };
        }
        // Fallback to sample data if no real data
        const weeks = ['Week 1', 'Week 2', 'Week 3', 'Week 4', 'Week 5', 'Week 6', 'Week 7'];
        return {
            labels: weeks,
            enrollments: [2, 3, 1, 4, 2, 3, 2],
            completions: [1, 1, 0, 2, 1, 1, 1]
        };
    }

    initializeCharts() {
        this.createUserGrowthChart();
        this.createCourseStatsChart();
        this.createRevenueTrendChart();
        this.createUserRolesChart();
        this.createEnrollmentActivityChart();
        this.createChatbotCharts();
    }



    createUserGrowthChart() {
        const ctx = document.getElementById('userGrowthChart');
        if (!ctx) return;

        const userGrowthData = this.getUserGrowthData();

        this.charts.userGrowth = new Chart(ctx.getContext('2d'), {
            type: 'line',
            data: {
                labels: userGrowthData.labels,
                datasets: [{
                    label: 'New Users',
                    data: userGrowthData.data,
                    borderColor: this.colors.primary,
                    backgroundColor: this.colors.primary + '15',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointBackgroundColor: this.colors.primary,
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        },
                        ticks: {
                            color: '#6c757d'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#6c757d'
                        }
                    }
                },
                elements: {
                    point: {
                        hoverRadius: 6
                    }
                }
            }
        });
    }

    createCourseStatsChart() {
        const ctx = document.getElementById('courseStatsChart');
        if (!ctx) return;

        this.charts.courseStats = new Chart(ctx.getContext('2d'), {
            type: 'doughnut',
            data: {
                labels: ['Approved', 'Pending', 'Rejected'],
                datasets: [{
                    data: [
                        this.adminData.approvedCourses,
                        this.adminData.pendingCourses,
                        this.adminData.rejectedCourses
                    ],
                    backgroundColor: [
                        this.colors.success,
                        this.colors.warning,
                        this.colors.danger
                    ],
                    borderWidth: 0,
                    cutout: '70%'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 20,
                            usePointStyle: true,
                            color: '#495057'
                        }
                    }
                }
            }
        });
    }

    createRevenueTrendChart() {
        const ctx = document.getElementById('revenueTrendChart');
        if (!ctx) return;

        const revenueData = this.getRevenueData();

        this.charts.revenueTrend = new Chart(ctx.getContext('2d'), {
            type: 'bar',
            data: {
                labels: revenueData.labels,
                datasets: [{
                    label: 'Revenue ($)',
                    data: revenueData.data,
                    backgroundColor: this.colors.info,
                    borderColor: this.colors.info,
                    borderWidth: 1,
                    borderRadius: 4,
                    borderSkipped: false,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        },
                        ticks: {
                            color: '#6c757d',
                            callback: function(value) {
                                return '$' + value.toLocaleString();
                            }
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#6c757d'
                        }
                    }
                }
            }
        });
    }

    createUserRolesChart() {
        const ctx = document.getElementById('userRolesChart');
        if (!ctx) return;

        // Ensure all values are positive and sum makes sense
        let learners = Math.max(1, this.adminData.totalLearners || 0);
        let instructors = Math.max(1, this.adminData.totalInstructors || 0);
        let admins = Math.max(1, this.adminData.totalAdmins || 0);
        
        // Validate that the sum doesn't exceed total users
        const sum = learners + instructors + admins;
        if (sum > this.adminData.totalUsers && this.adminData.totalUsers > 0) {
            const ratio = this.adminData.totalUsers / sum;
            learners = Math.max(1, Math.floor(learners * ratio));
            instructors = Math.max(1, Math.floor(instructors * ratio));
            admins = Math.max(1, this.adminData.totalUsers - learners - instructors);
        }

        this.charts.userRoles = new Chart(ctx.getContext('2d'), {
            type: 'pie',
            data: {
                labels: ['Learners', 'Instructors', 'Admins'],
                datasets: [{
                    data: [learners, instructors, admins],
                    backgroundColor: [
                        this.colors.blue,
                        this.colors.orange,
                        this.colors.purple
                    ],
                    borderWidth: 0,
                    cutout: '70%'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 20,
                            usePointStyle: true,
                            color: '#495057'
                        }
                    }
                }
            }
        });
    }

    createEnrollmentActivityChart() {
        const ctx = document.getElementById('enrollmentActivityChart');
        if (!ctx) return;

        const enrollmentData = this.getEnrollmentData();
        
        this.charts.enrollmentActivity = new Chart(ctx.getContext('2d'), {
            type: 'line',
            data: {
                labels: enrollmentData.labels,
                datasets: [{
                    label: 'New Enrollments',
                    data: enrollmentData.enrollments,
                    borderColor: this.colors.indigo,
                    backgroundColor: this.colors.indigo + '15',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointBackgroundColor: this.colors.indigo,
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 5
                }, {
                    label: 'Course Completions',
                    data: enrollmentData.completions,
                    borderColor: this.colors.success,
                    backgroundColor: this.colors.success + '15',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointBackgroundColor: this.colors.success,
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            padding: 20,
                            usePointStyle: true,
                            color: '#495057'
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        },
                        ticks: {
                            color: '#6c757d'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#6c757d'
                        }
                    }
                },
                elements: {
                    point: {
                        hoverRadius: 5
                    }
                }
            }
        });
    }

    initializeAnimations() {
        if (typeof AOS !== 'undefined') {
            AOS.init({
                duration: 600,
                easing: 'ease-in-out',
                once: true
            });
        }
    }

    // Public method to refresh charts with new data
    refreshCharts(newAdminData) {
        this.adminData = newAdminData;
        
        // Destroy existing charts
        Object.values(this.charts).forEach(chart => {
            if (chart) chart.destroy();
        });
        
        // Reinitialize charts
        this.initializeCharts();
    }

    // Public method to get chart instance
    getChart(chartName) {
        return this.charts[chartName];
    }

    // Chatbot Charts
    createChatbotCharts() {
        this.createChatbotDailyUsageChart();
        this.createChatbotFeedbackChart();
        this.createChatbotHourlyUsageChart();
    }

    createChatbotDailyUsageChart() {
        const ctx = document.getElementById('chatbotDailyUsageChart');
        if (!ctx) return;

        const dailyData = this.adminData.chatbotDailyUsage || [];
        
        this.charts.chatbotDailyUsage = new Chart(ctx.getContext('2d'), {
            type: 'line',
            data: {
                labels: dailyData.map(d => d.dateLabel),
                datasets: [{
                    label: 'Conversations',
                    data: dailyData.map(d => d.conversationCount),
                    borderColor: '#4f46e5',
                    backgroundColor: '#4f46e5' + '15',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointBackgroundColor: '#4f46e5',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 6
                }, {
                    label: 'Unique Users',
                    data: dailyData.map(d => d.uniqueUsers),
                    borderColor: '#06b6d4',
                    backgroundColor: '#06b6d4' + '15',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointBackgroundColor: '#06b6d4',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            padding: 20,
                            usePointStyle: true,
                            color: '#495057'
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        },
                        ticks: {
                            color: '#6c757d'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#6c757d'
                        }
                    }
                }
            }
        });
    }

    createChatbotFeedbackChart() {
        const ctx = document.getElementById('chatbotFeedbackChart');
        if (!ctx) return;

        const feedbackData = this.adminData.chatbotFeedback || [];
        
        this.charts.chatbotFeedback = new Chart(ctx.getContext('2d'), {
            type: 'bar',
            data: {
                labels: feedbackData.map(f => `${f.rating} Star${f.rating > 1 ? 's' : ''}`),
                datasets: [{
                    label: 'Feedback Count',
                    data: feedbackData.map(f => f.count),
                    backgroundColor: [
                        '#ef4444', // 1 star - red
                        '#f97316', // 2 stars - orange  
                        '#eab308', // 3 stars - yellow
                        '#22c55e', // 4 stars - green
                        '#16a34a'  // 5 stars - dark green
                    ],
                    borderWidth: 0,
                    borderRadius: 4,
                    borderSkipped: false,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        },
                        ticks: {
                            color: '#6c757d'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#6c757d'
                        }
                    }
                }
            }
        });
    }

    createChatbotHourlyUsageChart() {
        const ctx = document.getElementById('chatbotHourlyUsageChart');
        if (!ctx) return;

        const hourlyData = this.adminData.chatbotHourlyUsage || [];
        
        this.charts.chatbotHourlyUsage = new Chart(ctx.getContext('2d'), {
            type: 'line',
            data: {
                labels: hourlyData.map(h => h.hourLabel),
                datasets: [{
                    label: 'Conversations per Hour',
                    data: hourlyData.map(h => h.conversationCount),
                    borderColor: '#8b5cf6',
                    backgroundColor: '#8b5cf6' + '20',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.3,
                    pointBackgroundColor: '#8b5cf6',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        },
                        ticks: {
                            color: '#6c757d'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#6c757d',
                            maxTicksLimit: 12
                        }
                    }
                }
            }
        });
    }
}

// Initialize dashboard when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Check if admin data is available
    if (typeof window.adminData !== 'undefined') {
        window.adminDashboardCharts = new AdminDashboardCharts(window.adminData);
    } else {
        console.error('Admin data not available for charts initialization');
    }
});

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AdminDashboardCharts;
}