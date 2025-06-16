// Admin Dashboard JavaScript
class AdminDashboard {
    constructor() {
        this.charts = {};
        this.currentSection = 'overview';
        this.dashboardData = window.dashboardData || {};
        this.isLoading = false;
        
        this.init();
    }

    init() {
        this.initEventListeners();
        this.initSectionNavigation();
        this.initFilters();
        this.initCounterAnimations();
        this.initCharts();

        this.initMobileMenu();
        this.initAdminDropdown();
        this.initPerformanceOptimizations();
    }

    // Event Listeners
    initEventListeners() {
        // Refresh data button
        document.getElementById('refreshData')?.addEventListener('click', () => {
            this.refreshData();
        });

        // Time filter change
        document.getElementById('timeFilter')?.addEventListener('change', (e) => {
            this.handleTimeFilterChange(e.target.value);
        });

        // Section filters
        document.getElementById('userRoleFilter')?.addEventListener('change', (e) => {
            this.filterUserData(e.target.value);
        });

        document.getElementById('courseStatusFilter')?.addEventListener('change', (e) => {
            this.filterCourseData(e.target.value);
        });

        // Chart type controls
        document.querySelectorAll('[data-chart]').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const chartName = e.target.dataset.chart;
                const chartType = e.target.dataset.type;
                this.changeChartType(chartName, chartType);
                
                // Update button states
                e.target.parentElement.querySelectorAll('button').forEach(b => {
                    b.classList.remove('btn-primary');
                    b.classList.add('btn-outline-primary');
                });
                e.target.classList.remove('btn-outline-primary');
                e.target.classList.add('btn-primary');
            });
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.ctrlKey && e.key === 'r') {
                e.preventDefault();
                this.refreshData();
            }
        });
    }

    // Section Navigation
    initSectionNavigation() {
        document.querySelectorAll('.nav-link[data-section]').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const section = e.target.closest('.nav-link').dataset.section;
                this.showSection(section);
                
                // Update active state
                document.querySelectorAll('.nav-link').forEach(l => l.classList.remove('active'));
                e.target.closest('.nav-link').classList.add('active');
            });
        });
    }

    showSection(sectionName) {
        // Hide all sections
        document.querySelectorAll('.dashboard-section').forEach(section => {
            section.classList.remove('active');
        });

        // Show selected section
        const targetSection = document.getElementById(`${sectionName}-section`);
        if (targetSection) {
            targetSection.classList.add('active');
            this.currentSection = sectionName;
            
            // Lazy load charts for performance
            this.loadSectionCharts(sectionName);
        }
    }

    // Filters
    initFilters() {
        // Initialize date picker
        const dateRangeInput = document.getElementById('dateRange');
        if (dateRangeInput && typeof flatpickr !== 'undefined') {
            flatpickr(dateRangeInput, {
                mode: 'range',
                dateFormat: 'Y-m-d',
                onChange: (selectedDates) => {
                    if (selectedDates.length === 2) {
                        this.applyDateFilter(selectedDates[0], selectedDates[1]);
                    }
                }
            });
        }
    }

    handleTimeFilterChange(value) {
        const dateRangeInput = document.getElementById('dateRange');
        
        if (value === 'custom') {
            dateRangeInput.style.display = 'block';
        } else {
            dateRangeInput.style.display = 'none';
            this.applyTimeFilter(value);
        }
    }

    applyTimeFilter(period) {
        const now = new Date();
        let startDate;

        switch (period) {
            case '7days':
                startDate = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
                break;
            case '30days':
                startDate = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
                break;
            case '90days':
                startDate = new Date(now.getTime() - 90 * 24 * 60 * 60 * 1000);
                break;
            case '6months':
                startDate = new Date(now.getTime() - 180 * 24 * 60 * 60 * 1000);
                break;
            case '1year':
                startDate = new Date(now.getTime() - 365 * 24 * 60 * 60 * 1000);
                break;
            default:
                return;
        }

        this.filterDataByDateRange(startDate, now);
    }

    applyDateFilter(startDate, endDate) {
        this.filterDataByDateRange(startDate, endDate);
    }

    filterDataByDateRange(startDate, endDate) {
        this.showLoading();
        
        // Filter and update charts
        setTimeout(() => {
            this.updateChartsWithDateFilter(startDate, endDate);
            this.hideLoading();
        }, 500);
    }

    // Counter Animations
    initCounterAnimations() {
        const counters = document.querySelectorAll('.stat-value[data-count]');
        
        const animateCounter = (element) => {
            const target = parseInt(element.dataset.count) || 0;
            const duration = 2000;
            const increment = target / (duration / 16);
            let current = 0;

            const updateCounter = () => {
                if (current < target) {
                    current += increment;
                    element.textContent = Math.floor(current).toLocaleString();
                    requestAnimationFrame(updateCounter);
                } else {
                    element.textContent = target.toLocaleString();
                }
            };

            updateCounter();
        };

        // Intersection Observer for performance
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    animateCounter(entry.target);
                    observer.unobserve(entry.target);
                }
            });
        });

        counters.forEach(counter => observer.observe(counter));
    }

    // Chart Initialization
    initCharts() {
        this.loadSectionCharts('overview');
    }

    loadSectionCharts(section) {
        switch (section) {
            case 'overview':
                this.initOverviewCharts();
                break;
            case 'users':
                this.initUserCharts();
                break;
            case 'courses':
                this.initCourseCharts();
                break;
            case 'points':
                this.initPointCharts();
                break;
            case 'certificates':
                this.initCertificateCharts();
                break;
            case 'chatbot':
                this.initChatbotCharts();
                break;
        }
    }

    initOverviewCharts() {
        // Mini charts in stat cards
        this.createMiniChart('userMiniChart', this.dashboardData.userGrowthData, 'line');
        this.createMiniChart('courseMiniChart', this.dashboardData.userGrowthData, 'line');
        this.createMiniChart('pointsMiniChart', this.dashboardData.monthlyPointsData, 'line');
        this.createMiniChart('certificateMiniChart', this.dashboardData.certificateData, 'line');
        this.createMiniChart('revenueMiniChart', this.dashboardData.revenueData, 'line');
        this.createMiniChart('chatbotMiniChart', this.dashboardData.chatbotDailyUsage, 'line');

        // Main charts
        this.createUserGrowthChart();
        this.createUserRoleChart();
        this.createRevenueChart();
        this.createEnrollmentChart();
    }

    initUserCharts() {
        this.createUserActivityChart();
        this.createUserNewVsReturnChart();
    }

    initCourseCharts() {
        this.createCourseStatusChart();
        this.createPopularCoursesChart();
        this.createCourseCompletionChart();
    }

    initPointCharts() {
        this.createPointDistributionChart();
        this.createMonthlyPointsChart();
        this.createTopUsersPointsChart();
    }

    initCertificateCharts() {
        this.createCertificateMonthlyChart();
        this.createCertificateStatusChart();
        this.createCertificateRateChart();
    }

    initChatbotCharts() {
        this.createChatbotDailyChart();
        this.createChatbotFeedbackChart();
        this.createChatbotHourlyChart();
    }

    // Chart Creation Methods
    createMiniChart(canvasId, data, type) {
        const canvas = document.getElementById(canvasId);
        if (!canvas || !data || data.length === 0) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
        }

        this.charts[canvasId] = new Chart(ctx, {
            type: type,
            data: {
                labels: data.map(item => item.month || item.date),
                datasets: [{
                    data: data.map(item => item.newUsers || item.certificatesIssued || item.totalPointsEarned || item.revenue || item.conversationCount),
                    borderColor: '#667eea',
                    backgroundColor: 'rgba(102, 126, 234, 0.1)',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: { enabled: false }
                },
                scales: {
                    x: { display: false },
                    y: { display: false }
                },
                elements: {
                    point: { radius: 0 }
                },
                interaction: {
                    intersect: false
                }
            }
        });
    }

    createUserGrowthChart() {
        const canvas = document.getElementById('userGrowthChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts.userGrowthChart) {
            this.charts.userGrowthChart.destroy();
        }

        this.charts.userGrowthChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: this.dashboardData.userGrowthData?.map(item => item.month) || [],
                datasets: [{
                    label: 'New Users',
                    data: this.dashboardData.userGrowthData?.map(item => item.newUsers) || [],
                    borderColor: '#667eea',
                    backgroundColor: 'rgba(102, 126, 234, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            usePointStyle: true,
                            padding: 20
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        titleColor: 'white',
                        bodyColor: 'white',
                        borderColor: '#667eea',
                        borderWidth: 1
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { color: '#718096' }
                    },
                    y: {
                        grid: { color: 'rgba(0, 0, 0, 0.05)' },
                        ticks: { color: '#718096' }
                    }
                },
                interaction: {
                    intersect: false,
                    mode: 'index'
                }
            }
        });
    }

    createUserRoleChart() {
        const canvas = document.getElementById('userRoleChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts.userRoleChart) {
            this.charts.userRoleChart.destroy();
        }

        this.charts.userRoleChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Learners', 'Instructors', 'Administrators'],
                datasets: [{
                    data: [
                        this.dashboardData.totalLearners || 0,
                        this.dashboardData.totalInstructors || 0,
                        this.dashboardData.totalAdmins || 0
                    ],
                    backgroundColor: [
                        '#4299e1',
                        '#48bb78',
                        '#f56565'
                    ],
                    borderWidth: 0,
                    hoverOffset: 10
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            usePointStyle: true,
                            padding: 20
                        }
                    }
                }
            }
        });
    }

    createRevenueChart() {
        const canvas = document.getElementById('revenueChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts.revenueChart) {
            this.charts.revenueChart.destroy();
        }

        this.charts.revenueChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: this.dashboardData.revenueData?.map(item => item.month) || [],
                datasets: [{
                    label: 'Revenue (VND)',
                    data: this.dashboardData.revenueData?.map(item => item.revenue) || [],
                    backgroundColor: 'rgba(102, 126, 234, 0.8)',
                    borderColor: '#667eea',
                    borderWidth: 1,
                    borderRadius: 8,
                    borderSkipped: false
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
                    x: {
                        grid: { display: false },
                        ticks: { color: '#718096' }
                    },
                    y: {
                        grid: { color: 'rgba(0, 0, 0, 0.05)' },
                        ticks: { 
                            color: '#718096',
                            callback: function(value) {
                                return new Intl.NumberFormat('vi-VN', {
                                    style: 'currency',
                                    currency: 'VND'
                                }).format(value);
                            }
                        }
                    }
                }
            }
        });
    }

    createEnrollmentChart() {
        const canvas = document.getElementById('enrollmentChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts.enrollmentChart) {
            this.charts.enrollmentChart.destroy();
        }

        this.charts.enrollmentChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: this.dashboardData.enrollmentData?.map(item => item.week) || [],
                datasets: [{
                    label: 'New Enrollments',
                    data: this.dashboardData.enrollmentData?.map(item => item.newEnrollments) || [],
                    borderColor: '#48bb78',
                    backgroundColor: 'rgba(72, 187, 120, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4
                }, {
                    label: 'Completed',
                    data: this.dashboardData.enrollmentData?.map(item => item.completedCourses) || [],
                    borderColor: '#ed8936',
                    backgroundColor: 'rgba(237, 137, 54, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            usePointStyle: true,
                            padding: 20
                        }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { color: '#718096' }
                    },
                    y: {
                        grid: { color: 'rgba(0, 0, 0, 0.05)' },
                        ticks: { color: '#718096' }
                    }
                }
            }
        });
    }

    // Additional chart methods for other sections
    createUserActivityChart() {
        // Implementation for user activity chart
    }

    createCourseStatusChart() {
        const canvas = document.getElementById('courseStatusChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts.courseStatusChart) {
            this.charts.courseStatusChart.destroy();
        }

        this.charts.courseStatusChart = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: ['Approved', 'Pending', 'Rejected'],
                datasets: [{
                    data: [
                        this.dashboardData.approvedCourses || 0,
                        this.dashboardData.pendingCourses || 0,
                        this.dashboardData.rejectedCourses || 0
                    ],
                    backgroundColor: [
                        '#48bb78',
                        '#ed8936',
                        '#f56565'
                    ],
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            usePointStyle: true,
                            padding: 20
                        }
                    }
                }
            }
        });
    }

    // Point charts
    createPointDistributionChart() {
        const canvas = document.getElementById('pointDistributionChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts.pointDistributionChart) {
            this.charts.pointDistributionChart.destroy();
        }

        this.charts.pointDistributionChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: this.dashboardData.pointDistributionData?.map(item => item.pointRange) || [],
                datasets: [{
                    label: 'Number of Users',
                    data: this.dashboardData.pointDistributionData?.map(item => item.userCount) || [],
                    backgroundColor: 'rgba(246, 173, 85, 0.8)',
                    borderColor: '#f6ad55',
                    borderWidth: 1,
                    borderRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { color: '#718096' }
                    },
                    y: {
                        grid: { color: 'rgba(0, 0, 0, 0.05)' },
                        ticks: { color: '#718096' }
                    }
                }
            }
        });
    }

    createMonthlyPointsChart() {
        const canvas = document.getElementById('monthlyPointsChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts.monthlyPointsChart) {
            this.charts.monthlyPointsChart.destroy();
        }

        this.charts.monthlyPointsChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: this.dashboardData.monthlyPointsData?.map(item => item.month) || [],
                datasets: [{
                    label: 'Points Awarded',
                    data: this.dashboardData.monthlyPointsData?.map(item => item.totalPointsEarned) || [],
                    borderColor: '#f6ad55',
                    backgroundColor: 'rgba(246, 173, 85, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { color: '#718096' }
                    },
                    y: {
                        grid: { color: 'rgba(0, 0, 0, 0.05)' },
                        ticks: { color: '#718096' }
                    }
                }
            }
        });
    }

    // Certificate charts
    createCertificateMonthlyChart() {
        const canvas = document.getElementById('certificateMonthlyChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts.certificateMonthlyChart) {
            this.charts.certificateMonthlyChart.destroy();
        }

        this.charts.certificateMonthlyChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: this.dashboardData.certificateData?.map(item => item.month) || [],
                datasets: [{
                    label: 'Certificates Issued',
                    data: this.dashboardData.certificateData?.map(item => item.certificatesIssued) || [],
                    backgroundColor: 'rgba(159, 122, 234, 0.8)',
                    borderColor: '#9f7aea',
                    borderWidth: 1,
                    borderRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { color: '#718096' }
                    },
                    y: {
                        grid: { color: 'rgba(0, 0, 0, 0.05)' },
                        ticks: { color: '#718096' }
                    }
                }
            }
        });
    }

    createCertificateStatusChart() {
        const canvas = document.getElementById('certificateStatusChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts.certificateStatusChart) {
            this.charts.certificateStatusChart.destroy();
        }

        this.charts.certificateStatusChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Valid', 'Expired'],
                datasets: [{
                    data: [
                        this.dashboardData.validCertificates || 0,
                        this.dashboardData.expiredCertificates || 0
                    ],
                    backgroundColor: [
                        '#48bb78',
                        '#f56565'
                    ],
                    borderWidth: 0,
                    hoverOffset: 10
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            usePointStyle: true,
                            padding: 20
                        }
                    }
                }
            }
        });
    }

    // Chatbot charts
    createChatbotDailyChart() {
        const canvas = document.getElementById('chatbotDailyChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts.chatbotDailyChart) {
            this.charts.chatbotDailyChart.destroy();
        }

        this.charts.chatbotDailyChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: this.dashboardData.chatbotDailyUsage?.map(item => 
                    new Date(item.date).toLocaleDateString('vi-VN')
                ) || [],
                datasets: [{
                    label: 'Conversations',
                    data: this.dashboardData.chatbotDailyUsage?.map(item => item.conversationCount) || [],
                    borderColor: '#38b2ac',
                    backgroundColor: 'rgba(56, 178, 172, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4
                }, {
                    label: 'Unique Users',
                    data: this.dashboardData.chatbotDailyUsage?.map(item => item.uniqueUsers) || [],
                    borderColor: '#319795',
                    backgroundColor: 'rgba(49, 151, 149, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            usePointStyle: true,
                            padding: 20
                        }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { color: '#718096' }
                    },
                    y: {
                        grid: { color: 'rgba(0, 0, 0, 0.05)' },
                        ticks: { color: '#718096' }
                    }
                }
            }
        });
    }

    createChatbotFeedbackChart() {
        const canvas = document.getElementById('chatbotFeedbackChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts.chatbotFeedbackChart) {
            this.charts.chatbotFeedbackChart.destroy();
        }

        this.charts.chatbotFeedbackChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: this.dashboardData.chatbotFeedback?.map(item => `${item.rating} stars`) || [],
                datasets: [{
                    label: 'Number of Ratings',
                    data: this.dashboardData.chatbotFeedback?.map(item => item.count) || [],
                    backgroundColor: [
                        '#f56565',
                        '#ed8936',
                        '#ecc94b',
                        '#48bb78',
                        '#38a169'
                    ],
                    borderWidth: 0,
                    borderRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { color: '#718096' }
                    },
                    y: {
                        grid: { color: 'rgba(0, 0, 0, 0.05)' },
                        ticks: { color: '#718096' }
                    }
                }
            }
        });
    }

    createChatbotHourlyChart() {
        const canvas = document.getElementById('chatbotHourlyChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts.chatbotHourlyChart) {
            this.charts.chatbotHourlyChart.destroy();
        }

        this.charts.chatbotHourlyChart = new Chart(ctx, {
            type: 'radar',
            data: {
                labels: this.dashboardData.chatbotHourlyUsage?.map(item => `${item.hour}:00`) || [],
                datasets: [{
                    label: 'Number of Conversations',
                    data: this.dashboardData.chatbotHourlyUsage?.map(item => item.conversationCount) || [],
                    borderColor: '#38b2ac',
                    backgroundColor: 'rgba(56, 178, 172, 0.2)',
                    borderWidth: 2,
                    pointBackgroundColor: '#38b2ac',
                    pointBorderColor: '#fff',
                    pointHoverBackgroundColor: '#fff',
                    pointHoverBorderColor: '#38b2ac'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    r: {
                        angleLines: { color: 'rgba(0, 0, 0, 0.1)' },
                        grid: { color: 'rgba(0, 0, 0, 0.1)' },
                        pointLabels: { color: '#718096' },
                        ticks: { 
                            color: '#718096',
                            backdropColor: 'transparent'
                        }
                    }
                }
            }
        });
    }

    // Utility Methods
    changeChartType(chartName, type) {
        if (this.charts[chartName + 'Chart']) {
            const chart = this.charts[chartName + 'Chart'];
            chart.config.type = type;
            chart.update();
        }
    }

    showLoading() {
        const overlay = document.getElementById('loadingOverlay');
        if (overlay) {
            overlay.style.display = 'flex';
        }
        this.isLoading = true;
    }

    hideLoading() {
        const overlay = document.getElementById('loadingOverlay');
        if (overlay) {
            overlay.style.display = 'none';
        }
        this.isLoading = false;
    }

    refreshData() {
        if (this.isLoading) return;
        
        this.showLoading();
        
        // Simulate API call
        setTimeout(() => {
            // Refresh all charts in current section
            this.loadSectionCharts(this.currentSection);
            this.hideLoading();
            
            // Show success message
            this.showNotification('Data has been updated successfully!', 'success');
        }, 1000);
    }

    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `alert alert-${type} position-fixed`;
        notification.style.cssText = 'top: 20px; right: 20px; z-index: 10000; min-width: 300px;';
        notification.textContent = message;
        
        document.body.appendChild(notification);
        
        // Auto remove after 3 seconds
        setTimeout(() => {
            notification.remove();
        }, 3000);
    }

    // Mobile Menu
    initMobileMenu() {
        const toggleBtn = document.createElement('button');
        toggleBtn.className = 'mobile-menu-toggle';
        toggleBtn.innerHTML = '<i class="fas fa-bars"></i>';
        document.body.appendChild(toggleBtn);

        toggleBtn.addEventListener('click', () => {
            const sidebar = document.querySelector('.sidebar');
            sidebar.classList.toggle('open');
        });

        // Close sidebar when clicking outside
        document.addEventListener('click', (e) => {
            const sidebar = document.querySelector('.sidebar');
            const toggle = document.querySelector('.mobile-menu-toggle');
            
            if (!sidebar.contains(e.target) && !toggle.contains(e.target)) {
                sidebar.classList.remove('open');
            }
        });
    }

    // Admin Profile Dropdown
    initAdminDropdown() {
        const dropdown = document.querySelector('.admin-info.dropdown');
        const dropdownToggle = document.querySelector('#adminProfileDropdown');
        const dropdownMenu = document.querySelector('.admin-dropdown-menu');

        if (!dropdown || !dropdownToggle || !dropdownMenu) return;

        // Toggle dropdown on click
        dropdownToggle.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            
            const isOpen = dropdown.classList.contains('show');
            
            // Close all other dropdowns first
            document.querySelectorAll('.dropdown.show').forEach(d => {
                if (d !== dropdown) {
                    d.classList.remove('show');
                }
            });
            
            // Toggle current dropdown
            dropdown.classList.toggle('show', !isOpen);
        });

        // Close dropdown when clicking outside
        document.addEventListener('click', (e) => {
            if (!dropdown.contains(e.target)) {
                dropdown.classList.remove('show');
            }
        });

        // Close dropdown when pressing escape
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                dropdown.classList.remove('show');
            }
        });

        // Close dropdown when clicking on dropdown items
        dropdownMenu.addEventListener('click', (e) => {
            if (e.target.classList.contains('dropdown-item')) {
                dropdown.classList.remove('show');
            }
        });
    }



    // Performance Optimizations
    initPerformanceOptimizations() {
        // Debounce resize events
        let resizeTimeout;
        window.addEventListener('resize', () => {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(() => {
                this.handleResize();
            }, 250);
        });

        // Lazy load images
        const images = document.querySelectorAll('img[data-src]');
        const imageObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src;
                    img.removeAttribute('data-src');
                    imageObserver.unobserve(img);
                }
            });
        });

        images.forEach(img => imageObserver.observe(img));

        // Prefetch data for other sections
        this.prefetchSectionData();
    }

    handleResize() {
        // Resize all charts
        Object.keys(this.charts).forEach(chartKey => {
            if (this.charts[chartKey]) {
                this.charts[chartKey].resize();
            }
        });
    }

    prefetchSectionData() {
        // Implement data prefetching for better performance
    }

    updateChartsWithDateFilter(startDate, endDate) {
        // Filter data based on date range and update charts
        console.log('Filtering data from', startDate, 'to', endDate);
    }

    filterUserData(role) {
        // Filter user charts based on role
        console.log('Filtering user data by role:', role);
    }

    filterCourseData(status) {
        // Filter course charts based on status
        console.log('Filtering course data by status:', status);
    }
}

// Initialize dashboard when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new AdminDashboard();
});

// Export for testing purposes
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AdminDashboard;
} 