using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra_MVC.Models;

public partial class BrainStormEraContext : DbContext
{
    public BrainStormEraContext()
    {
    }

    public BrainStormEraContext(DbContextOptions<BrainStormEraContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Achievement> Achievements { get; set; }

    public virtual DbSet<AnswerOption> AnswerOptions { get; set; }

    public virtual DbSet<Certificate> Certificates { get; set; }

    public virtual DbSet<Chapter> Chapters { get; set; }

    public virtual DbSet<ChatbotConversation> ChatbotConversations { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseCategory> CourseCategories { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Lesson> Lessons { get; set; }

    public virtual DbSet<LessonPrerequisite> LessonPrerequisites { get; set; }

    public virtual DbSet<LessonType> LessonTypes { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<Quiz> Quizzes { get; set; }

    public virtual DbSet<QuizAttempt> QuizAttempts { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<UserAchievement> UserAchievements { get; set; }

    public virtual DbSet<UserAnswer> UserAnswers { get; set; }

    public virtual DbSet<UserProgress> UserProgresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__account__B9BE370F6E0666DE");

            entity.ToTable("account");

            entity.HasIndex(e => e.UserEmail, "UQ__account__B0FBA212EFEA1122").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__account__F3DBC57213E7EFD6").IsUnique();

            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");
            entity.Property(e => e.AccountCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("account_created_at");
            entity.Property(e => e.AccountUpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("account_updated_at");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.Gender).HasColumnName("gender");
            entity.Property(e => e.IsBanned)
                .HasDefaultValue(false)
                .HasColumnName("is_banned");
            entity.Property(e => e.LastLogin)
                .HasColumnType("datetime")
                .HasColumnName("last_login");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.PaymentPoint)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("payment_point");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("phone_number");
            entity.Property(e => e.UserAddress).HasColumnName("user_address");
            entity.Property(e => e.UserEmail)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("user_email");
            entity.Property(e => e.UserImage).HasColumnName("user_image");
            entity.Property(e => e.UserRole)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_role");
            entity.Property(e => e.Username)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("username");
        });

        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasKey(e => e.AchievementId).HasName("PK__achievem__3C492E83BC1F75D3");

            entity.ToTable("achievement");

            entity.Property(e => e.AchievementId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("achievement_id");
            entity.Property(e => e.AchievementCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("achievement_created_at");
            entity.Property(e => e.AchievementDescription).HasColumnName("achievement_description");
            entity.Property(e => e.AchievementIcon)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("achievement_icon");
            entity.Property(e => e.AchievementName)
                .HasMaxLength(255)
                .HasColumnName("achievement_name");
            entity.Property(e => e.AchievementType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("achievement_type");
            entity.Property(e => e.PointsReward)
                .HasDefaultValue(0)
                .HasColumnName("points_reward");
        });

        modelBuilder.Entity<AnswerOption>(entity =>
        {
            entity.HasKey(e => e.OptionId).HasName("PK__answer_o__F4EACE1B7FB0FEB7");

            entity.ToTable("answer_option");

            entity.Property(e => e.OptionId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("option_id");
            entity.Property(e => e.IsCorrect)
                .HasDefaultValue(false)
                .HasColumnName("is_correct");
            entity.Property(e => e.OptionOrder).HasColumnName("option_order");
            entity.Property(e => e.OptionText).HasColumnName("option_text");
            entity.Property(e => e.QuestionId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("question_id");

            entity.HasOne(d => d.Question).WithMany(p => p.AnswerOptions)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("FK__answer_op__quest__17036CC0");
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(e => e.CertificateId).HasName("PK__certific__E2256D3122DF0B33");

            entity.ToTable("certificate");

            entity.HasIndex(e => e.CertificateCode, "UQ__certific__2283DB566A541CB7").IsUnique();

            entity.Property(e => e.CertificateId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("certificate_id");
            entity.Property(e => e.CertificateCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("certificate_code");
            entity.Property(e => e.CertificateCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("certificate_created_at");
            entity.Property(e => e.CertificateName)
                .HasMaxLength(255)
                .HasColumnName("certificate_name");
            entity.Property(e => e.CertificateTemplate).HasColumnName("certificate_template");
            entity.Property(e => e.CertificateUrl).HasColumnName("certificate_url");
            entity.Property(e => e.CourseId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("course_id");
            entity.Property(e => e.EnrollmentId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("enrollment_id");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.FinalScore)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("final_score");
            entity.Property(e => e.IsValid)
                .HasDefaultValue(true)
                .HasColumnName("is_valid");
            entity.Property(e => e.IssueDate).HasColumnName("issue_date");
            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");
            entity.Property(e => e.VerificationUrl).HasColumnName("verification_url");

            entity.HasOne(d => d.Course).WithMany(p => p.Certificates)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__certifica__cours__42E1EEFE");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.Certificates)
                .HasForeignKey(d => d.EnrollmentId)
                .HasConstraintName("FK__certifica__enrol__40F9A68C");

            entity.HasOne(d => d.User).WithMany(p => p.Certificates)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__certifica__user___41EDCAC5");
        });

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(e => e.ChapterId).HasName("PK__chapter__745EFE87B5A84519");

            entity.ToTable("chapter");

            entity.HasIndex(e => new { e.CourseId, e.ChapterOrder }, "IX_chapter_course_order");

            entity.HasIndex(e => new { e.IsLocked, e.UnlockAfterChapterId }, "IX_chapter_unlock_status");

            entity.HasIndex(e => new { e.CourseId, e.ChapterOrder }, "unique_chapter_order_per_course").IsUnique();

            entity.Property(e => e.ChapterId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("chapter_id");
            entity.Property(e => e.ChapterCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("chapter_created_at");
            entity.Property(e => e.ChapterDescription).HasColumnName("chapter_description");
            entity.Property(e => e.ChapterName)
                .HasMaxLength(255)
                .HasColumnName("chapter_name");
            entity.Property(e => e.ChapterOrder).HasColumnName("chapter_order");
            entity.Property(e => e.ChapterStatus).HasColumnName("chapter_status");
            entity.Property(e => e.ChapterUpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("chapter_updated_at");
            entity.Property(e => e.CourseId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("course_id");
            entity.Property(e => e.IsLocked)
                .HasDefaultValue(false)
                .HasColumnName("is_locked");
            entity.Property(e => e.UnlockAfterChapterId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("unlock_after_chapter_id");

            entity.HasOne(d => d.ChapterStatusNavigation).WithMany(p => p.Chapters)
                .HasForeignKey(d => d.ChapterStatus)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__chapter__chapter__628FA481");

            entity.HasOne(d => d.Course).WithMany(p => p.Chapters)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK__chapter__course___619B8048");

            entity.HasOne(d => d.UnlockAfterChapter).WithMany(p => p.InverseUnlockAfterChapter)
                .HasForeignKey(d => d.UnlockAfterChapterId)
                .HasConstraintName("FK_chapter_unlock_after");
        });

        modelBuilder.Entity<ChatbotConversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId).HasName("PK__chatbot___311E7E9AB6236A96");

            entity.ToTable("chatbot_conversation");

            entity.Property(e => e.ConversationId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("conversation_id");
            entity.Property(e => e.BotResponse).HasColumnName("bot_response");
            entity.Property(e => e.ConversationContext).HasColumnName("conversation_context");
            entity.Property(e => e.ConversationTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("conversation_time");
            entity.Property(e => e.FeedbackRating).HasColumnName("feedback_rating");
            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");
            entity.Property(e => e.UserMessage).HasColumnName("user_message");

            entity.HasOne(d => d.User).WithMany(p => p.ChatbotConversations)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__chatbot_c__user___47A6A41B");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__course__8F1EF7AEC5B9682D");

            entity.ToTable("course");

            entity.HasIndex(e => e.AuthorId, "IX_course_author");

            entity.Property(e => e.CourseId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("course_id");
            entity.Property(e => e.AllowLessonPreview)
                .HasDefaultValue(false)
                .HasColumnName("allow_lesson_preview");
            entity.Property(e => e.ApprovalStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("approval_status");
            entity.Property(e => e.ApprovedAt)
                .HasColumnType("datetime")
                .HasColumnName("approved_at");
            entity.Property(e => e.ApprovedBy)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("approved_by");
            entity.Property(e => e.AuthorId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("author_id");
            entity.Property(e => e.CourseCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("course_created_at");
            entity.Property(e => e.CourseDescription).HasColumnName("course_description");
            entity.Property(e => e.CourseImage).HasColumnName("course_image");
            entity.Property(e => e.CourseName)
                .HasMaxLength(255)
                .HasColumnName("course_name");
            entity.Property(e => e.CourseStatus).HasColumnName("course_status");
            entity.Property(e => e.CourseUpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("course_updated_at");
            entity.Property(e => e.DifficultyLevel).HasColumnName("difficulty_level");
            entity.Property(e => e.EnforceSequentialAccess)
                .HasDefaultValue(true)
                .HasColumnName("enforce_sequential_access");
            entity.Property(e => e.EstimatedDuration).HasColumnName("estimated_duration");
            entity.Property(e => e.IsFeatured)
                .HasDefaultValue(false)
                .HasColumnName("is_featured");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.CourseApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__course__approved__571DF1D5");

            entity.HasOne(d => d.Author).WithMany(p => p.CourseAuthors)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__course__author_i__5629CD9C");

            entity.HasOne(d => d.CourseStatusNavigation).WithMany(p => p.Courses)
                .HasForeignKey(d => d.CourseStatus)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__course__course_s__5535A963");

            entity.HasMany(d => d.CourseCategories).WithMany(p => p.Courses)
                .UsingEntity<Dictionary<string, object>>(
                    "CourseCategoryMapping",
                    r => r.HasOne<CourseCategory>().WithMany()
                        .HasForeignKey("CourseCategoryId")
                        .HasConstraintName("FK__course_ca__cours__5AEE82B9"),
                    l => l.HasOne<Course>().WithMany()
                        .HasForeignKey("CourseId")
                        .HasConstraintName("FK__course_ca__cours__59FA5E80"),
                    j =>
                    {
                        j.HasKey("CourseId", "CourseCategoryId").HasName("PK__course_c__10F922209EC8546A");
                        j.ToTable("course_category_mapping");
                        j.IndexerProperty<string>("CourseId")
                            .HasMaxLength(36)
                            .IsUnicode(false)
                            .HasColumnName("course_id");
                        j.IndexerProperty<string>("CourseCategoryId")
                            .HasMaxLength(36)
                            .IsUnicode(false)
                            .HasColumnName("course_category_id");
                    });
        });

        modelBuilder.Entity<CourseCategory>(entity =>
        {
            entity.HasKey(e => e.CourseCategoryId).HasName("PK__course_c__FE7D58E8FB89006D");

            entity.ToTable("course_category");

            entity.Property(e => e.CourseCategoryId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("course_category_id");
            entity.Property(e => e.CategoryDescription).HasColumnName("category_description");
            entity.Property(e => e.CategoryIcon)
                .HasMaxLength(255)
                .HasColumnName("category_icon");
            entity.Property(e => e.CourseCategoryName)
                .HasMaxLength(255)
                .HasColumnName("course_category_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.UpdateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("update_at");
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__enrollme__6D24AA7A973F8AAE");

            entity.ToTable("enrollment");

            entity.HasIndex(e => new { e.EnrollmentStatus, e.UserId }, "IX_enrollment_status");

            entity.HasIndex(e => new { e.UserId, e.CourseId }, "IX_enrollment_user_course");

            entity.Property(e => e.EnrollmentId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("enrollment_id");
            entity.Property(e => e.Approved)
                .HasDefaultValue(false)
                .HasColumnName("approved");
            entity.Property(e => e.CertificateIssuedDate).HasColumnName("certificate_issued_date");
            entity.Property(e => e.CourseId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("course_id");
            entity.Property(e => e.CurrentLessonId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("current_lesson_id");
            entity.Property(e => e.EnrollmentCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("enrollment_created_at");
            entity.Property(e => e.EnrollmentStatus).HasColumnName("enrollment_status");
            entity.Property(e => e.EnrollmentUpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("enrollment_updated_at");
            entity.Property(e => e.LastAccessedLessonId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("last_accessed_lesson_id");
            entity.Property(e => e.ProgressPercentage)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("progress_percentage");
            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Course).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK__enrollmen__cours__7A672E12");

            entity.HasOne(d => d.CurrentLesson).WithMany(p => p.EnrollmentCurrentLessons)
                .HasForeignKey(d => d.CurrentLessonId)
                .HasConstraintName("FK_enrollment_current_lesson");

            entity.HasOne(d => d.EnrollmentStatusNavigation).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.EnrollmentStatus)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__enrollmen__enrol__7B5B524B");

            entity.HasOne(d => d.LastAccessedLesson).WithMany(p => p.EnrollmentLastAccessedLessons)
                .HasForeignKey(d => d.LastAccessedLessonId)
                .HasConstraintName("FK_enrollment_last_accessed_lesson");

            entity.HasOne(d => d.User).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__enrollmen__user___797309D9");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__feedback__7A6B2B8C533897E0");

            entity.ToTable("feedback");

            entity.HasIndex(e => new { e.CourseId, e.StarRating }, "IX_feedback_course_rating");

            entity.Property(e => e.FeedbackId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("feedback_id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CourseId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("course_id");
            entity.Property(e => e.FeedbackCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("feedback_created_at");
            entity.Property(e => e.FeedbackDate).HasColumnName("feedback_date");
            entity.Property(e => e.FeedbackUpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("feedback_updated_at");
            entity.Property(e => e.HelpfulCount)
                .HasDefaultValue(0)
                .HasColumnName("helpful_count");
            entity.Property(e => e.HiddenStatus)
                .HasDefaultValue(false)
                .HasColumnName("hidden_status");
            entity.Property(e => e.IsVerifiedPurchase)
                .HasDefaultValue(false)
                .HasColumnName("is_verified_purchase");
            entity.Property(e => e.StarRating).HasColumnName("star_rating");
            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Course).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK__feedback__course__503BEA1C");

            entity.HasOne(d => d.User).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__feedback__user_i__51300E55");
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.LessonId).HasName("PK__lesson__6421F7BE3A268BD6");

            entity.ToTable("lesson");

            entity.HasIndex(e => new { e.ChapterId, e.LessonOrder }, "IX_lesson_chapter_order");

            entity.HasIndex(e => new { e.IsLocked, e.UnlockAfterLessonId }, "IX_lesson_unlock_status");

            entity.HasIndex(e => new { e.ChapterId, e.LessonOrder }, "unique_lesson_order_per_chapter").IsUnique();

            entity.Property(e => e.LessonId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("lesson_id");
            entity.Property(e => e.AccessTime)
                .HasColumnType("datetime")
                .HasColumnName("access_time");
            entity.Property(e => e.ChapterId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("chapter_id");
            entity.Property(e => e.IsLocked)
                .HasDefaultValue(false)
                .HasColumnName("is_locked");
            entity.Property(e => e.IsMandatory)
                .HasDefaultValue(true)
                .HasColumnName("is_mandatory");
            entity.Property(e => e.LessonContent).HasColumnName("lesson_content");
            entity.Property(e => e.LessonCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("lesson_created_at");
            entity.Property(e => e.LessonDescription).HasColumnName("lesson_description");
            entity.Property(e => e.LessonName)
                .HasMaxLength(255)
                .HasColumnName("lesson_name");
            entity.Property(e => e.LessonOrder).HasColumnName("lesson_order");
            entity.Property(e => e.LessonStatus).HasColumnName("lesson_status");
            entity.Property(e => e.LessonTypeId).HasColumnName("lesson_type_id");
            entity.Property(e => e.LessonUpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("lesson_updated_at");
            entity.Property(e => e.MinCompletionPercentage)
                .HasDefaultValue(100.00m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("min_completion_percentage");
            entity.Property(e => e.MinQuizScore)
                .HasDefaultValue(70.00m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("min_quiz_score");
            entity.Property(e => e.MinTimeSpent)
                .HasDefaultValue(0)
                .HasColumnName("min_time_spent");
            entity.Property(e => e.RequiresQuizPass)
                .HasDefaultValue(false)
                .HasColumnName("requires_quiz_pass");
            entity.Property(e => e.UnlockAfterLessonId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("unlock_after_lesson_id");

            entity.HasOne(d => d.Chapter).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.ChapterId)
                .HasConstraintName("FK__lesson__chapter___6EF57B66");

            entity.HasOne(d => d.LessonStatusNavigation).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.LessonStatus)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__lesson__lesson_s__70DDC3D8");

            entity.HasOne(d => d.LessonType).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.LessonTypeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__lesson__lesson_t__6FE99F9F");

            entity.HasOne(d => d.UnlockAfterLesson).WithMany(p => p.InverseUnlockAfterLesson)
                .HasForeignKey(d => d.UnlockAfterLessonId)
                .HasConstraintName("FK_lesson_unlock_after");
        });

        modelBuilder.Entity<LessonPrerequisite>(entity =>
        {
            entity.HasKey(e => new { e.LessonId, e.PrerequisiteLessonId }).HasName("PK__lesson_p__ABB204EBA6ABE28A");

            entity.ToTable("lesson_prerequisite");

            entity.HasIndex(e => new { e.LessonId, e.PrerequisiteLessonId }, "IX_lesson_prerequisite_lookup");

            entity.Property(e => e.LessonId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("lesson_id");
            entity.Property(e => e.PrerequisiteLessonId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("prerequisite_lesson_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.PrerequisiteType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("completion")
                .HasColumnName("prerequisite_type");
            entity.Property(e => e.RequiredScore)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("required_score");
            entity.Property(e => e.RequiredTime).HasColumnName("required_time");

            entity.HasOne(d => d.Lesson).WithMany(p => p.LessonPrerequisiteLessons)
                .HasForeignKey(d => d.LessonId)
                .HasConstraintName("FK__lesson_pr__lesso__01142BA1");

            entity.HasOne(d => d.PrerequisiteLesson).WithMany(p => p.LessonPrerequisitePrerequisiteLessons)
                .HasForeignKey(d => d.PrerequisiteLessonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__lesson_pr__prere__02084FDA");
        });

        modelBuilder.Entity<LessonType>(entity =>
        {
            entity.HasKey(e => e.LessonTypeId).HasName("PK__lesson_t__F5960D1E30F01547");

            entity.ToTable("lesson_type");

            entity.Property(e => e.LessonTypeId)
                .ValueGeneratedNever()
                .HasColumnName("lesson_type_id");
            entity.Property(e => e.LessonTypeName)
                .HasMaxLength(255)
                .HasColumnName("lesson_type_name");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__notifica__E059842F5F162AF4");

            entity.ToTable("notification");

            entity.HasIndex(e => new { e.UserId, e.IsRead }, "IX_notification_user_unread");

            entity.Property(e => e.NotificationId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("notification_id");
            entity.Property(e => e.CourseId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("course_id");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.NotificationContent).HasColumnName("notification_content");
            entity.Property(e => e.NotificationCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("notification_created_at");
            entity.Property(e => e.NotificationTitle)
                .HasMaxLength(255)
                .HasColumnName("notification_title");
            entity.Property(e => e.NotificationType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("notification_type");
            entity.Property(e => e.ReadAt)
                .HasColumnType("datetime")
                .HasColumnName("read_at");
            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Course).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__notificat__cours__634EBE90");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.NotificationCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__notificat__creat__6442E2C9");

            entity.HasOne(d => d.User).WithMany(p => p.NotificationUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__notificat__user___625A9A57");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.PaymentMethodId).HasName("PK__payment___8A3EA9EB2B478CF5");

            entity.ToTable("payment_method");

            entity.Property(e => e.PaymentMethodId)
                .ValueGeneratedNever()
                .HasColumnName("payment_method_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MethodName)
                .HasMaxLength(100)
                .HasColumnName("method_name");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__payment___85C600AF37DC91CE");

            entity.ToTable("payment_transaction");

            entity.Property(e => e.TransactionId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("transaction_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CourseId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("course_id");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("VND")
                .HasColumnName("currency_code");
            entity.Property(e => e.ExchangeRate)
                .HasDefaultValue(1.0000m)
                .HasColumnType("decimal(10, 4)")
                .HasColumnName("exchange_rate");
            entity.Property(e => e.GatewayTransactionId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("gateway_transaction_id");
            entity.Property(e => e.NetAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("net_amount");
            entity.Property(e => e.PaymentDate)
                .HasColumnType("datetime")
                .HasColumnName("payment_date");
            entity.Property(e => e.PaymentGateway)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("payment_gateway");
            entity.Property(e => e.PaymentGatewayRef)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("payment_gateway_ref");
            entity.Property(e => e.PaymentMethodId).HasColumnName("payment_method_id");
            entity.Property(e => e.RefundAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("refund_amount");
            entity.Property(e => e.RefundDate)
                .HasColumnType("datetime")
                .HasColumnName("refund_date");
            entity.Property(e => e.RefundReason)
                .HasMaxLength(500)
                .HasColumnName("refund_reason");
            entity.Property(e => e.TransactionCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("transaction_created_at");
            entity.Property(e => e.TransactionFee)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("transaction_fee");
            entity.Property(e => e.TransactionStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("transaction_status");
            entity.Property(e => e.TransactionUpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("transaction_updated_at");
            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Course).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__payment_t__cours__2EDAF651");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__payment_t__payme__2FCF1A8A");

            entity.HasOne(d => d.User).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__payment_t__user___2DE6D218");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("PK__question__2EC2154906AF9851");

            entity.ToTable("question");

            entity.Property(e => e.QuestionId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("question_id");
            entity.Property(e => e.Explanation).HasColumnName("explanation");
            entity.Property(e => e.Points)
                .HasDefaultValue(1.00m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("points");
            entity.Property(e => e.QuestionCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("question_created_at");
            entity.Property(e => e.QuestionOrder).HasColumnName("question_order");
            entity.Property(e => e.QuestionText).HasColumnName("question_text");
            entity.Property(e => e.QuestionType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("question_type");
            entity.Property(e => e.QuizId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("quiz_id");

            entity.HasOne(d => d.Quiz).WithMany(p => p.Questions)
                .HasForeignKey(d => d.QuizId)
                .HasConstraintName("FK__question__quiz_i__1332DBDC");
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.QuizId).HasName("PK__quiz__2D7053EC7729EC43");

            entity.ToTable("quiz");

            entity.Property(e => e.QuizId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("quiz_id");
            entity.Property(e => e.BlocksLessonCompletion)
                .HasDefaultValue(false)
                .HasColumnName("blocks_lesson_completion");
            entity.Property(e => e.CourseId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("course_id");
            entity.Property(e => e.IsFinalQuiz)
                .HasDefaultValue(false)
                .HasColumnName("is_final_quiz");
            entity.Property(e => e.IsPrerequisiteQuiz)
                .HasDefaultValue(false)
                .HasColumnName("is_prerequisite_quiz");
            entity.Property(e => e.LessonId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("lesson_id");
            entity.Property(e => e.MaxAttempts)
                .HasDefaultValue(3)
                .HasColumnName("max_attempts");
            entity.Property(e => e.PassingScore)
                .HasDefaultValue(70.00m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("passing_score");
            entity.Property(e => e.QuizCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("quiz_created_at");
            entity.Property(e => e.QuizDescription).HasColumnName("quiz_description");
            entity.Property(e => e.QuizName)
                .HasMaxLength(255)
                .HasColumnName("quiz_name");
            entity.Property(e => e.QuizStatus).HasColumnName("quiz_status");
            entity.Property(e => e.QuizUpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("quiz_updated_at");
            entity.Property(e => e.TimeLimit).HasColumnName("time_limit");

            entity.HasOne(d => d.Course).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK__quiz__course_id__0C85DE4D");

            entity.HasOne(d => d.Lesson).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.LessonId)
                .HasConstraintName("FK__quiz__lesson_id__0B91BA14");

            entity.HasOne(d => d.QuizStatusNavigation).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.QuizStatus)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__quiz__quiz_statu__0D7A0286");
        });

        modelBuilder.Entity<QuizAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptId).HasName("PK__quiz_att__5621F949DDB9C1CD");

            entity.ToTable("quiz_attempt");

            entity.HasIndex(e => new { e.UserId, e.QuizId }, "IX_quiz_attempt_user_quiz");

            entity.Property(e => e.AttemptId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("attempt_id");
            entity.Property(e => e.AttemptNumber)
                .HasDefaultValue(1)
                .HasColumnName("attempt_number");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("end_time");
            entity.Property(e => e.IsPassed)
                .HasDefaultValue(false)
                .HasColumnName("is_passed");
            entity.Property(e => e.PercentageScore)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("percentage_score");
            entity.Property(e => e.QuizId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("quiz_id");
            entity.Property(e => e.Score)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("score");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("start_time");
            entity.Property(e => e.TimeSpent).HasColumnName("time_spent");
            entity.Property(e => e.TotalPoints)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("total_points");
            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Quiz).WithMany(p => p.QuizAttempts)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__quiz_atte__quiz___1CBC4616");

            entity.HasOne(d => d.User).WithMany(p => p.QuizAttempts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__quiz_atte__user___1BC821DD");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__status__3683B53192DF7120");

            entity.ToTable("status");

            entity.Property(e => e.StatusId)
                .ValueGeneratedNever()
                .HasColumnName("status_id");
            entity.Property(e => e.StatusName)
                .HasMaxLength(255)
                .HasColumnName("status_name");
        });

        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.AchievementId }).HasName("PK__user_ach__9A7AA5E7063E0488");

            entity.ToTable("user_achievement");

            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");
            entity.Property(e => e.AchievementId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("achievement_id");
            entity.Property(e => e.EnrollmentId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("enrollment_id");
            entity.Property(e => e.PointsEarned)
                .HasDefaultValue(0)
                .HasColumnName("points_earned");
            entity.Property(e => e.ReceivedDate).HasColumnName("received_date");
            entity.Property(e => e.RelatedCourseId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("related_course_id");

            entity.HasOne(d => d.Achievement).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.AchievementId)
                .HasConstraintName("FK__user_achi__achie__5AB9788F");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.EnrollmentId)
                .HasConstraintName("FK__user_achi__enrol__5BAD9CC8");

            entity.HasOne(d => d.RelatedCourse).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.RelatedCourseId)
                .HasConstraintName("FK__user_achi__relat__5CA1C101");

            entity.HasOne(d => d.User).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__user_achi__user___59C55456");
        });

        modelBuilder.Entity<UserAnswer>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.QuestionId, e.AttemptId }).HasName("PK__user_ans__730437A26BA3F80F");

            entity.ToTable("user_answer");

            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");
            entity.Property(e => e.QuestionId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("question_id");
            entity.Property(e => e.AttemptId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("attempt_id");
            entity.Property(e => e.AnswerText).HasColumnName("answer_text");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.PointsEarned)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("points_earned");
            entity.Property(e => e.SelectedOptionId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("selected_option_id");

            entity.HasOne(d => d.Attempt).WithMany(p => p.UserAnswers)
                .HasForeignKey(d => d.AttemptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__user_answ__attem__22751F6C");

            entity.HasOne(d => d.Question).WithMany(p => p.UserAnswers)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__user_answ__quest__2180FB33");

            entity.HasOne(d => d.SelectedOption).WithMany(p => p.UserAnswers)
                .HasForeignKey(d => d.SelectedOptionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__user_answ__selec__236943A5");

            entity.HasOne(d => d.User).WithMany(p => p.UserAnswers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__user_answ__user___208CD6FA");
        });

        modelBuilder.Entity<UserProgress>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LessonId }).HasName("PK__user_pro__4FFC28746535B036");

            entity.ToTable("user_progress");

            entity.HasIndex(e => new { e.UserId, e.IsCompleted, e.CompletedAt }, "IX_user_progress_completion");

            entity.HasIndex(e => new { e.UserId, e.IsUnlocked, e.UnlockedAt }, "IX_user_progress_unlock");

            entity.HasIndex(e => new { e.UserId, e.LessonId }, "IX_user_progress_user_lesson");

            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");
            entity.Property(e => e.LessonId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("lesson_id");
            entity.Property(e => e.AccessCount)
                .HasDefaultValue(0)
                .HasColumnName("access_count");
            entity.Property(e => e.CompletedAt)
                .HasColumnType("datetime")
                .HasColumnName("completed_at");
            entity.Property(e => e.FirstAccessedAt)
                .HasColumnType("datetime")
                .HasColumnName("first_accessed_at");
            entity.Property(e => e.IsCompleted)
                .HasDefaultValue(false)
                .HasColumnName("is_completed");
            entity.Property(e => e.IsUnlocked)
                .HasDefaultValue(false)
                .HasColumnName("is_unlocked");
            entity.Property(e => e.LastAccessedAt)
                .HasColumnType("datetime")
                .HasColumnName("last_accessed_at");
            entity.Property(e => e.MeetsPercentageRequirement)
                .HasDefaultValue(false)
                .HasColumnName("meets_percentage_requirement");
            entity.Property(e => e.MeetsQuizRequirement)
                .HasDefaultValue(true)
                .HasColumnName("meets_quiz_requirement");
            entity.Property(e => e.MeetsTimeRequirement)
                .HasDefaultValue(false)
                .HasColumnName("meets_time_requirement");
            entity.Property(e => e.ProgressPercentage)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("progress_percentage");
            entity.Property(e => e.TimeSpent)
                .HasDefaultValue(0)
                .HasColumnName("time_spent");
            entity.Property(e => e.UnlockedAt)
                .HasColumnType("datetime")
                .HasColumnName("unlocked_at");

            entity.HasOne(d => d.Lesson).WithMany(p => p.UserProgresses)
                .HasForeignKey(d => d.LessonId)
                .HasConstraintName("FK__user_prog__lesso__3B40CD36");

            entity.HasOne(d => d.User).WithMany(p => p.UserProgresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__user_prog__user___3A4CA8FD");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
