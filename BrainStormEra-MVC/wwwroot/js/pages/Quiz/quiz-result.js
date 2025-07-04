// Quiz Result JavaScript
class QuizResultManager {
  constructor() {
    this.bindEvents();
    this.initializeAnimations();
  }

  bindEvents() {
    // Toggle question review
    $("#toggle-review").on("click", () => this.toggleQuestionReview());

    // Toggle performance analysis
    $("#toggle-analysis").on("click", () => this.togglePerformanceAnalysis());

    // Question navigation
    $(".question-nav-item").on("click", (e) => this.navigateToQuestion(e));

    // Print results
    $("#print-results").on("click", () => this.printResults());

    // Share results (if implemented)
    $("#share-results").on("click", () => this.shareResults());
  }

  toggleQuestionReview() {
    const $review = $("#questions-review");
    const $button = $("#toggle-review");
    const $icon = $button.find("i");

    if ($review.is(":visible")) {
      $review.slideUp(300);
      $button.html('<i class="fas fa-eye"></i> Review Answers');
    } else {
      $review.slideDown(300);
      $button.html('<i class="fas fa-eye-slash"></i> Hide Review');

      // Animate question cards
      this.animateQuestionCards();
    }
  }

  togglePerformanceAnalysis() {
    const $analysis = $("#performance-analysis");
    const $button = $("#toggle-analysis");

    if ($analysis.is(":visible")) {
      $analysis.slideUp(300);
      $button.html('<i class="fas fa-chart-bar"></i> Show Analysis');
    } else {
      $analysis.slideDown(300);
      $button.html('<i class="fas fa-chart-bar"></i> Hide Analysis');

      // Animate analysis cards
      this.animateAnalysisCards();
    }
  }

  navigateToQuestion(e) {
    e.preventDefault();
    const questionIndex = $(e.currentTarget).data("question-index");
    const $questionCard = $(`.question-review-card:eq(${questionIndex})`);

    if ($questionCard.length) {
      $("html, body").animate(
        {
          scrollTop: $questionCard.offset().top - 100,
        },
        500
      );

      // Highlight the question temporarily
      $questionCard.addClass("highlight-question");
      setTimeout(() => {
        $questionCard.removeClass("highlight-question");
      }, 2000);
    }
  }

  initializeAnimations() {
    // Animate score on page load
    this.animateScore();

    // Animate result icon
    this.animateResultIcon();

    // Setup intersection observer for scroll animations
    this.setupScrollAnimations();
  }

  animateScore() {
    const $scoreValue = $(".main-score .score");
    const targetScore = parseFloat($scoreValue.text().replace("%", ""));

    if (!isNaN(targetScore)) {
      $scoreValue.text("0%");

      $({ score: 0 }).animate(
        { score: targetScore },
        {
          duration: 2000,
          easing: "easeOutCubic",
          step: function (now) {
            $scoreValue.text(Math.round(now) + "%");
          },
          complete: function () {
            $scoreValue.text(targetScore.toFixed(1) + "%");
          },
        }
      );
    }
  }

  animateResultIcon() {
    const $icon = $(".result-icon i");

    setTimeout(() => {
      $icon.addClass("animated-icon");
    }, 500);
  }

  animateQuestionCards() {
    $(".question-review-card").each(function (index) {
      $(this)
        .css({
          opacity: 0,
          transform: "translateY(20px)",
        })
        .delay(index * 100)
        .animate(
          {
            opacity: 1,
          },
          {
            duration: 500,
            step: function (now, fx) {
              if (fx.prop === "opacity") {
                $(this).css("transform", `translateY(${20 * (1 - now)}px)`);
              }
            },
          }
        );
    });
  }

  animateAnalysisCards() {
    $(".analysis-card").each(function (index) {
      $(this)
        .css({
          opacity: 0,
          transform: "scale(0.8)",
        })
        .delay(index * 150)
        .animate(
          {
            opacity: 1,
          },
          {
            duration: 600,
            step: function (now, fx) {
              if (fx.prop === "opacity") {
                const scale = 0.8 + 0.2 * now;
                $(this).css("transform", `scale(${scale})`);
              }
            },
          }
        );
    });
  }

  setupScrollAnimations() {
    if ("IntersectionObserver" in window) {
      const observerOptions = {
        threshold: 0.1,
        rootMargin: "0px 0px -50px 0px",
      };

      const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add("animate-in");
          }
        });
      }, observerOptions);

      // Observe elements
      document
        .querySelectorAll(".score-item, .question-review-card, .analysis-card")
        .forEach((el) => {
          observer.observe(el);
        });
    }
  }

  printResults() {
    // Hide elements that shouldn't be printed
    const $hiddenElements = $(".result-actions, .no-print");
    $hiddenElements.addClass("d-print-none");

    // Print the page
    window.print();

    // Restore elements after print
    setTimeout(() => {
      $hiddenElements.removeClass("d-print-none");
    }, 1000);
  }

  shareResults() {
    // Implementation for sharing results
    if (navigator.share) {
      navigator
        .share({
          title: "Quiz Results",
          text: `I scored ${$(".main-score .score").text()} on the quiz!`,
          url: window.location.href,
        })
        .catch(() => {
          // Error occurred
        });
    } else {
      // Fallback: copy URL to clipboard
      navigator.clipboard
        .writeText(window.location.href)
        .then(() => {
          this.showToast("Results URL copied to clipboard!", "success");
        })
        .catch(() => {
          // Fallback for older browsers
          const textArea = document.createElement("textarea");
          textArea.value = window.location.href;
          document.body.appendChild(textArea);
          textArea.select();
          document.execCommand("copy");
          document.body.removeChild(textArea);
          this.showToast("Results URL copied to clipboard!", "success");
        });
    }
  }

  showToast(message, type = "info") {
    const toast = $(`
            <div class="toast-notification toast-${type}">
                <span>${message}</span>
                <button type="button" class="close-toast">&times;</button>
            </div>
        `);

    $("body").append(toast);

    setTimeout(() => {
      toast.fadeOut(() => toast.remove());
    }, 3000);

    toast.find(".close-toast").on("click", () => {
      toast.fadeOut(() => toast.remove());
    });
  }

  // Question statistics
  generateQuestionStats() {
    const totalQuestions = $(".question-review-card").length;
    const correctAnswers = $(".question-review-card.correct").length;
    const incorrectAnswers = totalQuestions - correctAnswers;

    return {
      total: totalQuestions,
      correct: correctAnswers,
      incorrect: incorrectAnswers,
      percentage:
        totalQuestions > 0 ? (correctAnswers / totalQuestions) * 100 : 0,
    };
  }

  // Export results (if needed)
  exportResults() {
    const stats = this.generateQuestionStats();
    const results = {
      quizName: $(".quiz-title").text(),
      score: $(".main-score .score").text(),
      timestamp: new Date().toISOString(),
      statistics: stats,
    };

    const dataStr = JSON.stringify(results, null, 2);
    const dataBlob = new Blob([dataStr], { type: "application/json" });

    const link = document.createElement("a");
    link.href = URL.createObjectURL(dataBlob);
    link.download = "quiz-results.json";
    link.click();
  }

  // Accessibility improvements
  setupAccessibility() {
    // Add ARIA labels
    $(".question-review-card").each(function (index) {
      $(this).attr("aria-label", `Question ${index + 1} review`);
    });

    // Keyboard navigation for buttons
    $(".result-actions .btn").on("keydown", function (e) {
      if (e.key === "Enter" || e.key === " ") {
        e.preventDefault();
        $(this).click();
      }
    });
  }
}

// Initialize when document is ready
$(document).ready(function () {
  window.resultManager = new QuizResultManager();

  // Add some CSS animations
  $("<style>")
    .prop("type", "text/css")
    .html(
      `
            .animated-icon {
                animation: bounce 1s ease-in-out;
            }
            
            @keyframes bounce {
                0%, 20%, 60%, 100% {
                    transform: translateY(0);
                }
                40% {
                    transform: translateY(-10px);
                }
                80% {
                    transform: translateY(-5px);
                }
            }
            
            .highlight-question {
                background: #fff3cd !important;
                border-color: #ffc107 !important;
                transform: scale(1.02);
                transition: all 0.3s ease;
            }
            
            .animate-in {
                animation: slideInUp 0.6s ease-out;
            }
            
            @keyframes slideInUp {
                from {
                    opacity: 0;
                    transform: translateY(30px);
                }
                to {
                    opacity: 1;
                    transform: translateY(0);
                }
            }
            
            .toast-notification {
                position: fixed;
                top: 20px;
                right: 20px;
                padding: 15px 20px;
                border-radius: 8px;
                color: white;
                z-index: 1050;
                display: flex;
                align-items: center;
                gap: 10px;
                min-width: 250px;
                box-shadow: 0 4px 15px rgba(0, 0, 0, 0.2);
            }
            
            .toast-success {
                background: linear-gradient(135deg, #28a745, #20c997);
            }
            
            .toast-info {
                background: linear-gradient(135deg, #007bff, #0056b3);
            }
            
            .close-toast {
                background: none;
                border: none;
                color: inherit;
                font-size: 18px;
                cursor: pointer;
                padding: 0;
                margin-left: auto;
            }
            
            @media print {
                .result-actions,
                .no-print {
                    display: none !important;
                }
                
                .quiz-result-container {
                    background: white !important;
                    box-shadow: none !important;
                }
                
                .question-review-card {
                    page-break-inside: avoid;
                    margin-bottom: 20px;
                }
            }
        `
    )
    .appendTo("head");
});

// Smooth scrolling for anchor links
$(document).on("click", 'a[href^="#"]', function (e) {
  e.preventDefault();
  const target = $(this.getAttribute("href"));
  if (target.length) {
    $("html, body").animate(
      {
        scrollTop: target.offset().top - 100,
      },
      500
    );
  }
});
