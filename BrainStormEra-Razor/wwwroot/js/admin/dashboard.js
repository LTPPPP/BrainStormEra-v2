// ==============================================
// SIMPLIFIED ADMIN DASHBOARD JAVASCRIPT
// ==============================================

// Global variables
let charts = {};
let chartInstances = {};
let isLoading = false;

// Chart.js configuration
document.addEventListener("DOMContentLoaded", function () {
  // Set Chart.js defaults
  if (typeof Chart !== 'undefined') {
    Chart.defaults.font.family = "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
    Chart.defaults.font.size = 5;
    Chart.defaults.color = '#64748b';
  }
  
  console.log("Dashboard initializing...");
  
  initializeCharts();
  setupEventListeners();
  loadAllChartData();
  
  console.log("Dashboard initialized successfully");
});

// Color schemes
const colorSchemes = {
  primary: ['#4f46e5', '#6366f1', '#8b5cf6', '#a855f7'],
  success: ['#10b981', '#059669', '#047857', '#065f46'],
  warning: ['#f59e0b', '#d97706', '#b45309', '#92400e'],
  danger: ['#ef4444', '#dc2626', '#b91c1c', '#991b1b'],
  info: ['#06b6d4', '#0891b2', '#0e7490', '#155e75']
};

// ==============================================
// EVENT LISTENERS SETUP
// ==============================================

function setupEventListeners() {
  // Logout confirmation
  const logoutBtn = document.querySelector(".btn-logout");
  if (logoutBtn) {
    logoutBtn.addEventListener("click", handleLogoutClick);
  }



    // Auto-refresh toggle
  document.addEventListener("keydown", function(e) {
    if (e.ctrlKey && e.key === 'r') {
      e.preventDefault();
      refreshDashboard();
    }
  });

  // Resize handler for responsive charts
  window.addEventListener('resize', debounce(handleResize, 250));
}



// ==============================================
// CHART INITIALIZATION
// ==============================================

function initializeCharts() {
  console.log("Initializing charts with data:", window.dashboardData);
  
  // Initialize each chart
  initializeUsersChart();
  initializeCoursesChart();
  initializeCertificatesChart();
  initializePointsChart();
  initializeChatbotChart();
  initializeRevenueChart();
}

function initializeUsersChart() {
  const ctx = document.getElementById("usersChart");
  if (!ctx) return;

  const data = window.dashboardData || {};
  
  charts.users = new Chart(ctx, {
    type: 'doughnut',
    data: {
      labels: ['Learners', 'Instructors', 'Admins'],
      datasets: [{
        data: [
          data.totalLearners || 0,
          data.totalInstructors || 0,
          data.totalAdmins || 0
        ],
        backgroundColor: colorSchemes.primary,
        borderWidth: 1,
        borderColor: '#ffffff',
        hoverBorderWidth: 2,
        hoverOffset: 5
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          position: 'bottom',
          labels: {
            padding: 7,
            usePointStyle: true,
            font: {
              size: 5,
              weight: '600'
            }
          }
        },
        tooltip: {
          backgroundColor: 'rgba(0, 0, 0, 0.8)',
          titleColor: '#ffffff',
          bodyColor: '#ffffff',
          borderColor: '#4f46e5',
          borderWidth: 1,
          cornerRadius: 5,
          callbacks: {
            label: function(context) {
              const total = context.dataset.data.reduce((a, b) => a + b, 0);
              const percentage = total > 0 ? ((context.parsed / total) * 100).toFixed(1) : '0';
              return `${context.label}: ${context.parsed.toLocaleString()} (${percentage}%)`;
            }
          }
        }
      },
      animation: {
        animateRotate: true,
        animateScale: true,
        duration: 1000,
        easing: 'easeOutQuart'
      }
    }
  });

  chartInstances.users = charts.users;
}

function initializeCoursesChart() {
  const ctx = document.getElementById("coursesChart");
  if (!ctx) return;

  const data = window.dashboardData || {};
  
  charts.courses = new Chart(ctx, {
    type: 'bar',
    data: {
      labels: ['Approved', 'Pending', 'Rejected'],
      datasets: [{
        label: 'Courses',
        data: [
          data.approvedCourses || 0,
          data.pendingCourses || 0,
          data.rejectedCourses || 0
        ],
        backgroundColor: [
          colorSchemes.success[0],
          colorSchemes.warning[0],
          colorSchemes.danger[0]
        ],
        borderColor: [
          colorSchemes.success[1],
          colorSchemes.warning[1],
          colorSchemes.danger[1]
        ],
        borderWidth: 2,
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
        },
        tooltip: {
          backgroundColor: 'rgba(0, 0, 0, 0.8)',
          titleColor: '#ffffff',
          bodyColor: '#ffffff',
          borderColor: '#10b981',
          borderWidth: 2,
          cornerRadius: 10,
          callbacks: {
            title: function(context) {
              return `${context[0].label} Courses`;
            },
            label: function(context) {
              return `Count: ${context.parsed.y.toLocaleString()}`;
            }
          }
        }
      },
      scales: {
        y: {
          beginAtZero: true,
          grid: {
            color: 'rgba(156, 163, 175, 0.1)'
          },
          ticks: {
            color: '#6b7280',
            font: {
              size: 12
            }
          }
        },
        x: {
          grid: {
            display: false
          },
          ticks: {
            color: '#6b7280',
            font: {
              size: 12,
              weight: '600'
            }
          }
        }
      },
      animation: {
        duration: 2000,
        easing: 'easeOutQuart'
      }
    }
  });

  chartInstances.courses = charts.courses;
}

function initializeCertificatesChart() {
  const ctx = document.getElementById("certificatesChart");
  if (!ctx) return;

  const data = window.dashboardData || {};
  const certificateData = data.certificateData || [];
  
  charts.certificates = new Chart(ctx, {
    type: 'line',
    data: {
      labels: certificateData.map(c => c.month) || ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
      datasets: [{
        label: 'Certificates Issued',
        data: certificateData.map(c => c.certificatesIssued) || [10, 15, 12, 20, 18, 25],
        borderColor: colorSchemes.warning[0],
        backgroundColor: function(context) {
          const chart = context.chart;
          const {ctx, chartArea} = chart;
          if (!chartArea) return null;
          
          const gradient = ctx.createLinearGradient(0, chartArea.top, 0, chartArea.bottom);
          gradient.addColorStop(0, 'rgba(245, 158, 11, 0.2)');
          gradient.addColorStop(1, 'rgba(245, 158, 11, 0.02)');
          return gradient;
        },
        borderWidth: 3,
        fill: true,
        tension: 0.4,
        pointBackgroundColor: '#ffffff',
        pointBorderColor: colorSchemes.warning[0],
        pointBorderWidth: 3,
        pointRadius: 6,
        pointHoverRadius: 8
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      interaction: {
        intersect: false,
        mode: 'index'
      },
      plugins: {
        legend: {
          display: false
        },
        tooltip: {
          backgroundColor: 'rgba(0, 0, 0, 0.8)',
          titleColor: '#ffffff',
          bodyColor: '#ffffff',
          borderColor: colorSchemes.warning[0],
          borderWidth: 2,
          cornerRadius: 10,
          callbacks: {
            title: function(context) {
              return `${context[0].label} 2024`;
            },
            label: function(context) {
              return `Certificates: ${context.parsed.y.toLocaleString()}`;
            }
          }
        }
      },
      scales: {
        y: {
          beginAtZero: true,
          grid: {
            color: 'rgba(156, 163, 175, 0.1)'
          },
          ticks: {
            color: '#6b7280',
            font: {
              size: 12
            }
          }
        },
        x: {
          grid: {
            display: false
          },
          ticks: {
            color: '#6b7280',
            font: {
              size: 12
            }
          }
        }
      },
      animation: {
        duration: 2000,
        easing: 'easeOutQuart'
      }
    }
  });

  chartInstances.certificates = charts.certificates;
}

function initializePointsChart() {
  const ctx = document.getElementById("pointsChart");
  if (!ctx) return;

  const data = window.dashboardData || {};
  const pointsData = data.monthlyPointsData || [];
  
  charts.points = new Chart(ctx, {
    type: 'bar',
    data: {
      labels: pointsData.map(p => p.month) || ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
      datasets: [{
        label: 'Points Earned',
        data: pointsData.map(p => p.totalPointsEarned) || [1200, 1500, 1300, 1800, 1600, 2000],
        backgroundColor: colorSchemes.danger[0],
        borderColor: colorSchemes.danger[0],
        borderWidth: 2,
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
        },
        tooltip: {
          backgroundColor: 'rgba(0, 0, 0, 0.8)',
          titleColor: '#ffffff',
          bodyColor: '#ffffff',
          borderColor: colorSchemes.danger[0],
          borderWidth: 2,
          cornerRadius: 10,
          callbacks: {
            title: function(context) {
              return `${context[0].label} 2024`;
            },
            label: function(context) {
              return `Points: ${context.parsed.y.toLocaleString()}`;
            }
          }
        }
      },
      scales: {
        y: {
          beginAtZero: true,
          grid: {
            color: 'rgba(156, 163, 175, 0.1)'
          },
          ticks: {
            color: '#6b7280',
            font: {
              size: 12
            },
            callback: function(value) {
              return value.toLocaleString();
            }
          }
        },
        x: {
          grid: {
            display: false
          },
          ticks: {
            color: '#6b7280',
            font: {
              size: 12
            }
          }
        }
      },
      animation: {
        duration: 2000,
        easing: 'easeOutQuart'
      }
    }
  });

  chartInstances.points = charts.points;
}

function initializeChatbotChart() {
  const ctx = document.getElementById("chatbotChart");
  if (!ctx) return;

  const data = window.dashboardData || {};
  const chatbotData = data.chatbotDailyUsage || [];
  
  charts.chatbot = new Chart(ctx, {
    type: 'line',
    data: {
      labels: chatbotData.map(c => {
        try {
          return new Date(c.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
        } catch {
          return c.date || 'N/A';
        }
      }) || ['1/1', '1/2', '1/3', '1/4', '1/5', '1/6', '1/7'],
      datasets: [{
        label: 'Daily Conversations',
        data: chatbotData.map(c => c.conversationCount) || [5, 8, 12, 7, 15, 10, 18],
        borderColor: colorSchemes.info[0],
        backgroundColor: function(context) {
          const chart = context.chart;
          const {ctx, chartArea} = chart;
          if (!chartArea) return null;
          
          const gradient = ctx.createLinearGradient(0, chartArea.top, 0, chartArea.bottom);
          gradient.addColorStop(0, 'rgba(6, 182, 212, 0.2)');
          gradient.addColorStop(1, 'rgba(6, 182, 212, 0.02)');
          return gradient;
        },
        borderWidth: 3,
        fill: true,
        tension: 0.4,
        pointBackgroundColor: '#ffffff',
        pointBorderColor: colorSchemes.info[0],
        pointBorderWidth: 3,
        pointRadius: 6,
        pointHoverRadius: 8
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      interaction: {
        intersect: false,
        mode: 'index'
      },
      plugins: {
        legend: {
          display: false
        },
        tooltip: {
          backgroundColor: 'rgba(0, 0, 0, 0.8)',
          titleColor: '#ffffff',
          bodyColor: '#ffffff',
          borderColor: colorSchemes.info[0],
          borderWidth: 2,
          cornerRadius: 10,
          callbacks: {
            title: function(context) {
              return `Date: ${context[0].label}`;
            },
            label: function(context) {
              return `Conversations: ${context.parsed.y}`;
            }
          }
        }
      },
      scales: {
        y: {
          beginAtZero: true,
          grid: {
            color: 'rgba(156, 163, 175, 0.1)'
          },
          ticks: {
            color: '#6b7280',
            font: {
              size: 12
            }
          }
        },
        x: {
          grid: {
            display: false
          },
          ticks: {
            color: '#6b7280',
            font: {
              size: 12
            }
          }
        }
      },
      animation: {
        duration: 2000,
        easing: 'easeOutQuart'
      }
    }
  });

  chartInstances.chatbot = charts.chatbot;
}

function initializeRevenueChart() {
  const ctx = document.getElementById("revenueChart");
  if (!ctx) return;

  const data = window.dashboardData || {};
  const revenueData = data.revenueData || [];
  
  charts.revenue = new Chart(ctx, {
    type: 'line',
    data: {
      labels: revenueData.map(r => r.month) || ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
      datasets: [{
        label: 'Monthly Revenue',
        data: revenueData.map(r => r.revenue) || [5000, 7500, 6200, 8900, 7800, 12000],
        borderColor: '#8b5cf6',
        backgroundColor: function(context) {
          const chart = context.chart;
          const {ctx, chartArea} = chart;
          if (!chartArea) return null;
          
          const gradient = ctx.createLinearGradient(0, chartArea.top, 0, chartArea.bottom);
          gradient.addColorStop(0, 'rgba(139, 92, 246, 0.2)');
          gradient.addColorStop(1, 'rgba(139, 92, 246, 0.02)');
          return gradient;
        },
        borderWidth: 3,
        fill: true,
        tension: 0.4,
        pointBackgroundColor: '#ffffff',
        pointBorderColor: '#8b5cf6',
        pointBorderWidth: 3,
        pointRadius: 6,
        pointHoverRadius: 8
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      interaction: {
        intersect: false,
        mode: 'index'
      },
      plugins: {
        legend: {
          display: false
        },
        tooltip: {
          backgroundColor: 'rgba(0, 0, 0, 0.8)',
          titleColor: '#ffffff',
          bodyColor: '#ffffff',
          borderColor: '#8b5cf6',
          borderWidth: 2,
          cornerRadius: 10,
          callbacks: {
            title: function(context) {
              return `${context[0].label} 2024`;
            },
            label: function(context) {
              return `Revenue: $${context.parsed.y.toLocaleString()}`;
            }
          }
        }
      },
      scales: {
        y: {
          beginAtZero: true,
          grid: {
            color: 'rgba(156, 163, 175, 0.1)'
          },
          ticks: {
            color: '#6b7280',
            font: {
              size: 12
            },
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
            color: '#6b7280',
            font: {
              size: 12
            }
          }
        }
      },
      animation: {
        duration: 2000,
        easing: 'easeOutQuart'
      }
    }
  });

  chartInstances.revenue = charts.revenue;
}

// ==============================================
// CHART INTERACTION FUNCTIONS
// ==============================================

function changeChartView(chartType, viewType) {
  const chart = chartInstances[chartType];
  if (!chart) return;

  // Update view buttons
  const container = document.querySelector(`[data-chart="${chartType}"]`);
  if (container) {
    container.querySelectorAll('.view-btn').forEach(btn => btn.classList.remove('active'));
    container.querySelector(`[data-view="${viewType}"]`)?.classList.add('active');
  }

  // Handle special chart types
  let actualChartType = viewType;
  let fillOption = chart.config.data.datasets[0]?.fill || false;
  
  if (viewType === 'area') {
    actualChartType = 'line';
    fillOption = true;
  }
  
  if (viewType === 'pie') {
    actualChartType = 'doughnut';
  }

  // Change chart type and configuration
  chart.config.type = actualChartType;
  
  // Update fill option for area charts
  if (chart.config.data.datasets && chart.config.data.datasets[0]) {
    chart.config.data.datasets[0].fill = fillOption;
  }
  
  chart.update('active');
  
  showToast(`Chart view changed to ${viewType}`, 'success');
}

function refreshChart(chartType) {
  showLoading(chartType);
  
  setTimeout(() => {
    loadChartData(chartType);
    hideLoading(chartType);
    showToast(`${chartType} chart refreshed`, 'success');
  }, 1000);
}

function exportChart(chartType) {
  const chart = chartInstances[chartType];
  if (!chart) return;

  const url = chart.toBase64Image();
  const link = document.createElement('a');
  link.download = `${chartType}-chart.png`;
  link.href = url;
  link.click();
  
  showToast(`${chartType} chart exported`, 'success');
}

// ==============================================
// DATA LOADING FUNCTIONS
// ==============================================

async function loadAllChartData() {
  const chartTypes = ['users', 'courses', 'certificates', 'points', 'chatbot', 'revenue'];
  
  for (const chartType of chartTypes) {
    hideLoading(chartType);
  }
}

async function loadChartData(chartType, year = null, month = null, category = null) {
  try {
    const url = new URL('/admin', window.location.origin);
    url.searchParams.append('handler', 'ChartData');
    url.searchParams.append('chartType', chartType);
    
    if (year) url.searchParams.append('year', year);
    if (month) url.searchParams.append('month', month);
    if (category) url.searchParams.append('category', category);

    const response = await fetch(url, {
      method: 'GET',
      headers: {
        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
      }
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    updateChart(chartType, data);
    
  } catch (error) {
    console.error(`Error loading ${chartType} chart data:`, error);
    showChartError(chartType);
  }
}

function updateChart(chartType, data) {
  const chart = chartInstances[chartType];
  if (!chart) return;

  switch (chartType) {
    case 'users':
      chart.data.datasets[0].data = [
        data.totalLearners || 0,
        data.totalInstructors || 0,
        data.totalAdmins || 0
      ];
      break;
      
    case 'courses':
      chart.data.datasets[0].data = [
        data.approvedCourses || 0,
        data.pendingCourses || 0,
        data.rejectedCourses || 0
      ];
      break;
      
    case 'certificates':
      if (data.certificateData) {
        chart.data.labels = data.certificateData.map(c => c.month);
        chart.data.datasets[0].data = data.certificateData.map(c => c.certificatesIssued);
      }
      break;
      
    case 'points':
      if (data.monthlyData) {
        chart.data.labels = data.monthlyData.map(m => m.month);
        chart.data.datasets[0].data = data.monthlyData.map(m => m.totalPointsEarned);
      }
      break;
      
    case 'chatbot':
      if (data.dailyUsage) {
        chart.data.labels = data.dailyUsage.map(d => {
          try {
            return new Date(d.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
          } catch {
            return d.date || 'N/A';
          }
        });
        chart.data.datasets[0].data = data.dailyUsage.map(d => d.conversationCount);
      }
      break;
      
    case 'revenue':
      if (data.revenueData) {
        chart.data.labels = data.revenueData.map(r => r.month);
        chart.data.datasets[0].data = data.revenueData.map(r => r.revenue);
      }
      break;
  }

  chart.update('active');
}

// ==============================================
// FILTER FUNCTIONS
// ==============================================



// ==============================================
// ACTIVITY TAB FUNCTIONS
// ==============================================

function showActivityTab(tabName) {
  // Update tab buttons
  document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
  const activeTab = document.querySelector(`[onclick="showActivityTab('${tabName}')"]`);
  if (activeTab) activeTab.classList.add('active');
  
  // Show/hide content
  document.querySelectorAll('.activity-tab-content').forEach(content => content.style.display = 'none');
  const activeContent = document.getElementById(`${tabName}-activity`);
  if (activeContent) activeContent.style.display = 'block';
}

// ==============================================
// UTILITY FUNCTIONS
// ==============================================

function showLoading(chartType) {
  if (chartType === 'all') {
    document.querySelectorAll('.chart-loading').forEach(loading => {
      loading.style.display = 'flex';
    });
  } else {
    const loading = document.getElementById(`${chartType}ChartLoading`);
    if (loading) loading.style.display = 'flex';
  }
}

function hideLoading(chartType) {
  if (chartType === 'all') {
    document.querySelectorAll('.chart-loading').forEach(loading => {
      loading.style.display = 'none';
    });
  } else {
    const loading = document.getElementById(`${chartType}ChartLoading`);
    if (loading) loading.style.display = 'none';
  }
}

function showChartError(chartType) {
  const canvas = document.getElementById(`${chartType}Chart`);
  if (canvas) {
    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.fillStyle = '#ef4444';
    ctx.font = '16px Inter, sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText('Error loading chart data', canvas.width / 2, canvas.height / 2);
  }
}

function showToast(message, type = 'info') {
  const toast = document.createElement('div');
  toast.className = `toast toast-${type}`;
  toast.innerHTML = `
    <div class="toast-content">
      <i class="fas ${getToastIcon(type)}"></i>
      <span>${message}</span>
    </div>
  `;
  
  document.body.appendChild(toast);
  
  // Animate in
  setTimeout(() => toast.classList.add('show'), 100);
  
  // Auto remove
  setTimeout(() => {
    toast.classList.remove('show');
    setTimeout(() => {
      if (document.body.contains(toast)) {
        document.body.removeChild(toast);
      }
    }, 300);
  }, 3000);
}

function getToastIcon(type) {
  switch (type) {
    case 'success': return 'fa-check-circle';
    case 'error': return 'fa-exclamation-circle';
    case 'warning': return 'fa-exclamation-triangle';
    default: return 'fa-info-circle';
  }
}

function handleResize() {
  Object.values(chartInstances).forEach(chart => {
    if (chart && typeof chart.resize === 'function') {
      chart.resize();
    }
  });
}

function debounce(func, wait) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

// ==============================================
// GLOBAL FUNCTIONS
// ==============================================

function refreshDashboard() {
  showToast('Refreshing dashboard...', 'info');
  loadAllChartData();
}

function exportData() {
  const data = {
    timestamp: new Date().toISOString(),
    dashboardData: window.dashboardData,
    charts: Object.keys(chartInstances)
  };
  
  const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.download = 'dashboard-data.json';
  link.href = url;
  link.click();
  URL.revokeObjectURL(url);
  
  showToast('Dashboard data exported', 'success');
}

function handleLogoutClick(event) {
  if (!confirm("Are you sure you want to logout?")) {
    event.preventDefault();
  }
}

// ==============================================
// ERROR HANDLING
// ==============================================

window.addEventListener('error', function(e) {
  console.error('Dashboard error:', e.error);
  showToast('An error occurred. Please refresh the page.', 'error');
});

window.addEventListener('unhandledrejection', function(e) {
  console.error('Unhandled promise rejection:', e.reason);
  showToast('Connection error. Please check your internet.', 'error');
});

console.log("Dashboard JavaScript loaded successfully");
