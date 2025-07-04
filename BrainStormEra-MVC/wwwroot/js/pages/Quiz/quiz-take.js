// Quiz Take JavaScript
class QuizTakeManager {
  constructor() {
    this.quizData = window.quizData || {};
    this.timer = null;
    this.timeRemaining = 0;
    this.autoSubmitTimer = null;
    this.isSubmitting = false;

    this.initializeQuiz();
    this.bindEvents();
    this.updateProgress();

    if (this.quizData.timeLimit) {
      this.startTimer();
    }
  }

  initializeQuiz() {
    // Calculate time remaining
    if (this.quizData.timeLimit && this.quizData.startTime) {
      const now = new Date();
      const startTime = new Date(this.quizData.startTime);
      const elapsed = (now - startTime) / 1000 / 60; // minutes
      this.timeRemaining = Math.max(0, this.quizData.timeLimit - elapsed);

      if (this.timeRemaining <= 0) {
        this.autoSubmitQuiz();
        return;
      }
    }

    // Set submission time
    this.updateSubmissionTime();

    // Auto-save functionality (if needed)
    this.setupAutoSave();
  }

  bindEvents() {
    // Form submission
    $("#quiz-form").on("submit", (e) => this.handleSubmit(e));

    // Answer selection changes
    $('input[type="radio"], textarea').on("change input", () => {
      this.updateProgress();
      this.updateSubmissionTime();
    });

    // Submit button click
    $("#submit-quiz").on("click", (e) => this.showSubmitConfirmation(e));

    // Modal confirmations
    $("#confirm-submit").on("click", () => this.confirmSubmit());
    $("#confirm-auto-submit").on("click", () => this.confirmSubmit());

    // Prevent accidental page leave
    window.addEventListener("beforeunload", (e) => {
      if (!this.isSubmitting && this.hasAnswers()) {
        e.preventDefault();
        e.returnValue =
          "You have unsaved quiz answers. Are you sure you want to leave?";
        return e.returnValue;
      }
    });

    // Save draft (if implemented)
    $("#save-draft").on("click", () => this.saveDraft());

    // Keyboard shortcuts
    $(document).on("keydown", (e) => this.handleKeyboardShortcuts(e));
  }

  startTimer() {
    if (!this.quizData.timeLimit || this.timeRemaining <= 0) return;

    this.updateTimerDisplay();

    this.timer = setInterval(() => {
      this.timeRemaining -= 1 / 60; // Decrease by 1 second (in minutes)

      if (this.timeRemaining <= 0) {
        this.timeRemaining = 0;
        this.autoSubmitQuiz();
        return;
      }

      this.updateTimerDisplay();
      this.checkTimeWarnings();
    }, 1000);
  }

  updateTimerDisplay() {
    const minutes = Math.floor(this.timeRemaining);
    const seconds = Math.floor((this.timeRemaining - minutes) * 60);
    const display = `${minutes}:${seconds.toString().padStart(2, "0")}`;

    $("#time-remaining").text(display);
  }

  checkTimeWarnings() {
    const $timer = $("#quiz-timer");
    const $warning = $("#timer-warning");

    if (this.timeRemaining <= 1) {
      // 1 minute warning
      $timer.addClass("danger");
      $warning.show();
    } else if (this.timeRemaining <= 5) {
      // 5 minute warning
      $timer.addClass("warning");
    }
  }

  autoSubmitQuiz() {
    clearInterval(this.timer);

    // Show auto-submit modal
    $("#auto-submit-modal").modal({
      backdrop: "static",
      keyboard: false,
    });

    // Auto-submit after 3 seconds if user doesn't click OK
    this.autoSubmitTimer = setTimeout(() => {
      this.confirmSubmit();
    }, 3000);
  }

  updateProgress() {
    const totalQuestions = this.quizData.totalQuestions || 0;
    let answeredQuestions = 0;

    // Count answered multiple choice questions
    $('input[type="radio"]:checked').each(function () {
      answeredQuestions++;
    });

    // Count answered text questions
    $("textarea").each(function () {
      if ($(this).val().trim() !== "") {
        answeredQuestions++;
      }
    });

    const percentage =
      totalQuestions > 0 ? (answeredQuestions / totalQuestions) * 100 : 0;

    $("#progress-fill").css("width", percentage + "%");
    $("#progress-text").text(
      `${answeredQuestions} of ${totalQuestions} questions answered`
    );
  }

  updateSubmissionTime() {
    $("#submission-time").val(new Date().toISOString());
  }

  hasAnswers() {
    return (
      $('input[type="radio"]:checked').length > 0 ||
      $("textarea").filter(function () {
        return $(this).val().trim() !== "";
      }).length > 0
    );
  }

  getUnansweredCount() {
    const totalQuestions = this.quizData.totalQuestions || 0;
    let answeredQuestions = 0;

    // Count answered questions
    $('input[type="radio"]:checked').each(function () {
      answeredQuestions++;
    });

    $("textarea").each(function () {
      if ($(this).val().trim() !== "") {
        answeredQuestions++;
      }
    });

    return totalQuestions - answeredQuestions;
  }

  showSubmitConfirmation(e) {
    e.preventDefault();

    const unansweredCount = this.getUnansweredCount();

    if (unansweredCount > 0) {
      $("#unanswered-count").text(unansweredCount);
      $("#unanswered-warning").show();
    } else {
      $("#unanswered-warning").hide();
    }

    $("#submit-confirmation-modal").modal("show");
  }

  confirmSubmit() {
    // Clear auto-submit timer if exists
    if (this.autoSubmitTimer) {
      clearTimeout(this.autoSubmitTimer);
    }

    // Hide modals
    $(".modal").modal("hide");

    // Mark as submitting to prevent beforeunload warning
    this.isSubmitting = true;

    // Update submission time one last time
    this.updateSubmissionTime();

    // Submit the form
    $("#quiz-form")[0].submit();
  }

  handleSubmit(e) {
    if (!this.isSubmitting) {
      e.preventDefault();
      this.showSubmitConfirmation(e);
    }
  }

  handleKeyboardShortcuts(e) {
    // Ctrl+S or Cmd+S for save draft
    if ((e.ctrlKey || e.metaKey) && e.key === "s") {
      e.preventDefault();
      this.saveDraft();
    }

    // Ctrl+Enter or Cmd+Enter for submit
    if ((e.ctrlKey || e.metaKey) && e.key === "Enter") {
      e.preventDefault();
      this.showSubmitConfirmation(e);
    }
  }

  saveDraft() {
    // Implementation for saving draft (if needed)
    // This could save answers to localStorage or send to server

    // Show toast notification
    this.showToast("Draft saved successfully", "success");
  }

  setupAutoSave() {
    // Auto-save every 30 seconds (if needed)
    if (this.autoSaveEnabled) {
      setInterval(() => {
        this.saveDraft();
      }, 30000);
    }
  }

  showToast(message, type = "info") {
    // Create toast notification
    const toast = $(`
            <div class="toast-notification toast-${type}">
                <span>${message}</span>
                <button type="button" class="close-toast">&times;</button>
            </div>
        `);

    $("body").append(toast);

    // Auto-remove after 3 seconds
    setTimeout(() => {
      toast.fadeOut(() => toast.remove());
    }, 3000);

    // Manual close
    toast.find(".close-toast").on("click", () => {
      toast.fadeOut(() => toast.remove());
    });
  }

  // Question navigation methods
  scrollToQuestion(questionIndex) {
    const $question = $(".question-card").eq(questionIndex);
    if ($question.length) {
      $("html, body").animate(
        {
          scrollTop: $question.offset().top - 100,
        },
        500
      );
    }
  }

  highlightUnansweredQuestions() {
    $(".question-card").removeClass("unanswered-highlight");

    $(".question-card").each(function () {
      const $card = $(this);
      const hasRadioAnswer =
        $card.find('input[type="radio"]:checked').length > 0;
      const hasTextAnswer = $card.find("textarea").val().trim() !== "";

      if (!hasRadioAnswer && !hasTextAnswer) {
        $card.addClass("unanswered-highlight");
      }
    });
  }

  // Accessibility features
  setupAccessibility() {
    // Add ARIA labels
    $('input[type="radio"]').each(function (index) {
      $(this).attr("aria-label", `Option ${String.fromCharCode(65 + index)}`);
    });

    // Add keyboard navigation for options
    $(".answer-option label").on("keydown", function (e) {
      if (e.key === "Enter" || e.key === " ") {
        e.preventDefault();
        $(this).prev("input").click();
      }
    });
  }

  // Clean up when page unloads
  cleanup() {
    if (this.timer) {
      clearInterval(this.timer);
    }
    if (this.autoSubmitTimer) {
      clearTimeout(this.autoSubmitTimer);
    }
  }
}

// Initialize quiz when document is ready
$(document).ready(function () {
  window.quizManager = new QuizTakeManager();

  // Cleanup on page unload
  $(window).on("beforeunload", function () {
    if (window.quizManager) {
      window.quizManager.cleanup();
    }
  });
});

// CSS for toast notifications (can be moved to CSS file)
$("<style>")
  .prop("type", "text/css")
  .html(
    `
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
        
        .toast-warning {
            background: linear-gradient(135deg, #ffc107, #e0a800);
            color: #212529;
        }
        
        .toast-error {
            background: linear-gradient(135deg, #dc3545, #c82333);
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
        
        .unanswered-highlight {
            border: 2px solid #ffc107 !important;
            box-shadow: 0 0 10px rgba(255, 193, 7, 0.3) !important;
        }
    `
  )
  .appendTo("head");
