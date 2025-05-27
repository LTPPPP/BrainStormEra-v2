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

    public virtual DbSet<Conversation> Conversations { get; set; }

    public virtual DbSet<ConversationParticipant> ConversationParticipants { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseCategory> CourseCategories { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Lesson> Lessons { get; set; }

    public virtual DbSet<LessonPrerequisite> LessonPrerequisites { get; set; }

    public virtual DbSet<LessonType> LessonTypes { get; set; }

    public virtual DbSet<MessageEntity> MessageEntities { get; set; }

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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=LTP; Database=BrainStormEra;User ID=sa;Password=01654460072ltp;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__account__B9BE370FB87C7C29");

            entity.ToTable("account");

            entity.HasIndex(e => e.UserEmail, "UQ__account__B0FBA212A4ACF4F4").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__account__F3DBC572AE042B5C").IsUnique();

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
            entity.HasKey(e => e.AchievementId).HasName("PK__achievem__3C492E83CFFD004A");

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
            entity.HasKey(e => e.OptionId).HasName("PK__answer_o__F4EACE1B5B9F61F1");

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
                .HasConstraintName("FK__answer_op__quest__29221CFB");
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(e => e.CertificateId).HasName("PK__certific__E2256D31CA75D648");

            entity.ToTable("certificate");

            entity.HasIndex(e => e.CertificateCode, "UQ__certific__2283DB568A8248D4").IsUnique();

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
                .HasConstraintName("FK__certifica__cours__55009F39");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.Certificates)
                .HasForeignKey(d => d.EnrollmentId)
                .HasConstraintName("FK__certifica__enrol__531856C7");

            entity.HasOne(d => d.User).WithMany(p => p.Certificates)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__certifica__user___540C7B00");
        });

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(e => e.ChapterId).HasName("PK__chapter__745EFE87A2B1A813");

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
                .HasConstraintName("FK__chapter__chapter__74AE54BC");

            entity.HasOne(d => d.Course).WithMany(p => p.Chapters)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK__chapter__course___73BA3083");

            entity.HasOne(d => d.UnlockAfterChapter).WithMany(p => p.InverseUnlockAfterChapter)
                .HasForeignKey(d => d.UnlockAfterChapterId)
                .HasConstraintName("FK_chapter_unlock_after");
        });

        modelBuilder.Entity<ChatbotConversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId).HasName("PK__chatbot___311E7E9AA555FB96");

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
                .HasConstraintName("FK__chatbot_c__user___59C55456");
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId).HasName("PK__conversa__311E7E9A6C6C27D7");

            entity.ToTable("conversation");

            entity.HasIndex(e => e.LastMessageAt, "IX_conversation_last_message").IsDescending();

            entity.Property(e => e.ConversationId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("conversation_id");
            entity.Property(e => e.ConversationCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("conversation_created_at");
            entity.Property(e => e.ConversationName)
                .HasMaxLength(255)
                .HasColumnName("conversation_name");
            entity.Property(e => e.ConversationType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("private")
                .HasColumnName("conversation_type");
            entity.Property(e => e.ConversationUpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("conversation_updated_at");
            entity.Property(e => e.CourseId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("course_id");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastMessageAt)
                .HasColumnType("datetime")
                .HasColumnName("last_message_at");
            entity.Property(e => e.LastMessageId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("last_message_id");

            entity.HasOne(d => d.Course).WithMany(p => p.Conversations)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__conversat__cours__0B5CAFEA");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Conversations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__conversat__creat__0A688BB1");

            entity.HasOne(d => d.LastMessage).WithMany(p => p.Conversations)
                .HasForeignKey(d => d.LastMessageId)
                .HasConstraintName("FK__conversat__last___0C50D423");
        });

        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.HasKey(e => new { e.ConversationId, e.UserId }).HasName("PK__conversa__DA859DEA7A64ABCF");

            entity.ToTable("conversation_participant");

            entity.HasIndex(e => new { e.UserId, e.IsActive }, "IX_conversation_participants");

            entity.Property(e => e.ConversationId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("conversation_id");
            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsMuted)
                .HasDefaultValue(false)
                .HasColumnName("is_muted");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("joined_at");
            entity.Property(e => e.LastReadAt)
                .HasColumnType("datetime")
                .HasColumnName("last_read_at");
            entity.Property(e => e.LastReadMessageId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("last_read_message_id");
            entity.Property(e => e.LeftAt)
                .HasColumnType("datetime")
                .HasColumnName("left_at");
            entity.Property(e => e.ParticipantRole)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("member")
                .HasColumnName("participant_role");

            entity.HasOne(d => d.Conversation).WithMany(p => p.ConversationParticipants)
                .HasForeignKey(d => d.ConversationId)
                .HasConstraintName("FK__conversat__conve__13F1F5EB");

            entity.HasOne(d => d.LastReadMessage).WithMany(p => p.ConversationParticipants)
                .HasForeignKey(d => d.LastReadMessageId)
                .HasConstraintName("FK__conversat__last___15DA3E5D");

            entity.HasOne(d => d.User).WithMany(p => p.ConversationParticipants)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__conversat__user___14E61A24");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__course__8F1EF7AE8E6D31FA");

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
                .HasConstraintName("FK__course__approved__693CA210");

            entity.HasOne(d => d.Author).WithMany(p => p.CourseAuthors)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__course__author_i__68487DD7");

            entity.HasOne(d => d.CourseStatusNavigation).WithMany(p => p.Courses)
                .HasForeignKey(d => d.CourseStatus)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__course__course_s__6754599E");

            entity.HasMany(d => d.CourseCategories).WithMany(p => p.Courses)
                .UsingEntity<Dictionary<string, object>>(
                    "CourseCategoryMapping",
                    r => r.HasOne<CourseCategory>().WithMany()
                        .HasForeignKey("CourseCategoryId")
                        .HasConstraintName("FK__course_ca__cours__6D0D32F4"),
                    l => l.HasOne<Course>().WithMany()
                        .HasForeignKey("CourseId")
                        .HasConstraintName("FK__course_ca__cours__6C190EBB"),
                    j =>
                    {
                        j.HasKey("CourseId", "CourseCategoryId").HasName("PK__course_c__10F92220593CE068");
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
            entity.HasKey(e => e.CourseCategoryId).HasName("PK__course_c__FE7D58E81D526588");

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
            entity.HasKey(e => e.EnrollmentId).HasName("PK__enrollme__6D24AA7A9F2698DA");

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
                .HasConstraintName("FK__enrollmen__cours__0C85DE4D");

            entity.HasOne(d => d.CurrentLesson).WithMany(p => p.EnrollmentCurrentLessons)
                .HasForeignKey(d => d.CurrentLessonId)
                .HasConstraintName("FK_enrollment_current_lesson");

            entity.HasOne(d => d.EnrollmentStatusNavigation).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.EnrollmentStatus)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__enrollmen__enrol__0D7A0286");

            entity.HasOne(d => d.LastAccessedLesson).WithMany(p => p.EnrollmentLastAccessedLessons)
                .HasForeignKey(d => d.LastAccessedLessonId)
                .HasConstraintName("FK_enrollment_last_accessed_lesson");

            entity.HasOne(d => d.User).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__enrollmen__user___0B91BA14");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__feedback__7A6B2B8C7B5F4325");

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
                .HasConstraintName("FK__feedback__course__625A9A57");

            entity.HasOne(d => d.User).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__feedback__user_i__634EBE90");
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.LessonId).HasName("PK__lesson__6421F7BEB53BDADC");

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
                .HasConstraintName("FK__lesson__chapter___01142BA1");

            entity.HasOne(d => d.LessonStatusNavigation).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.LessonStatus)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__lesson__lesson_s__02FC7413");

            entity.HasOne(d => d.LessonType).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.LessonTypeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__lesson__lesson_t__02084FDA");

            entity.HasOne(d => d.UnlockAfterLesson).WithMany(p => p.InverseUnlockAfterLesson)
                .HasForeignKey(d => d.UnlockAfterLessonId)
                .HasConstraintName("FK_lesson_unlock_after");
        });

        modelBuilder.Entity<LessonPrerequisite>(entity =>
        {
            entity.HasKey(e => new { e.LessonId, e.PrerequisiteLessonId }).HasName("PK__lesson_p__ABB204EBBEB9E5DA");

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
                .HasConstraintName("FK__lesson_pr__lesso__1332DBDC");

            entity.HasOne(d => d.PrerequisiteLesson).WithMany(p => p.LessonPrerequisitePrerequisiteLessons)
                .HasForeignKey(d => d.PrerequisiteLessonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__lesson_pr__prere__14270015");
        });

        modelBuilder.Entity<LessonType>(entity =>
        {
            entity.HasKey(e => e.LessonTypeId).HasName("PK__lesson_t__F5960D1E20DA2AC3");

            entity.ToTable("lesson_type");

            entity.Property(e => e.LessonTypeId)
                .ValueGeneratedNever()
                .HasColumnName("lesson_type_id");
            entity.Property(e => e.LessonTypeName)
                .HasMaxLength(255)
                .HasColumnName("lesson_type_name");
        });

        modelBuilder.Entity<MessageEntity>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__message___0BBF6EE6BCD71717");

            entity.ToTable("message_entity");

            entity.HasIndex(e => new { e.ConversationId, e.MessageCreatedAt }, "IX_message_conversation");

            entity.HasIndex(e => e.ReplyToMessageId, "IX_message_reply_to");

            entity.HasIndex(e => new { e.SenderId, e.ReceiverId }, "IX_message_sender_receiver");

            entity.HasIndex(e => new { e.ReceiverId, e.IsRead, e.MessageCreatedAt }, "IX_message_unread");

            entity.Property(e => e.MessageId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("message_id");
            entity.Property(e => e.AttachmentName)
                .HasMaxLength(255)
                .HasColumnName("attachment_name");
            entity.Property(e => e.AttachmentSize).HasColumnName("attachment_size");
            entity.Property(e => e.AttachmentUrl).HasColumnName("attachment_url");
            entity.Property(e => e.ConversationId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("conversation_id");
            entity.Property(e => e.EditedAt)
                .HasColumnType("datetime")
                .HasColumnName("edited_at");
            entity.Property(e => e.IsDeletedByReceiver)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted_by_receiver");
            entity.Property(e => e.IsDeletedBySender)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted_by_sender");
            entity.Property(e => e.IsEdited)
                .HasDefaultValue(false)
                .HasColumnName("is_edited");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.MessageContent).HasColumnName("message_content");
            entity.Property(e => e.MessageCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("message_created_at");
            entity.Property(e => e.MessageType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("text")
                .HasColumnName("message_type");
            entity.Property(e => e.MessageUpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("message_updated_at");
            entity.Property(e => e.OriginalContent).HasColumnName("original_content");
            entity.Property(e => e.ReadAt)
                .HasColumnType("datetime")
                .HasColumnName("read_at");
            entity.Property(e => e.ReceiverId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("receiver_id");
            entity.Property(e => e.ReplyToMessageId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("reply_to_message_id");
            entity.Property(e => e.SenderId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("sender_id");

            entity.HasOne(d => d.Receiver).WithMany(p => p.MessageEntityReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__message_e__recei__01D345B0");

            entity.HasOne(d => d.ReplyToMessage).WithMany(p => p.InverseReplyToMessage)
                .HasForeignKey(d => d.ReplyToMessageId)
                .HasConstraintName("FK__message_e__reply__02C769E9");

            entity.HasOne(d => d.Sender).WithMany(p => p.MessageEntitySenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__message_e__sende__00DF2177");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__notifica__E059842F0F3A73A4");

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
                .HasConstraintName("FK__notificat__cours__756D6ECB");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.NotificationCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__notificat__creat__76619304");

            entity.HasOne(d => d.User).WithMany(p => p.NotificationUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__notificat__user___74794A92");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.PaymentMethodId).HasName("PK__payment___8A3EA9EBB53AB20C");

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
            entity.HasKey(e => e.TransactionId).HasName("PK__payment___85C600AFA91EEA9B");

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
                .HasConstraintName("FK__payment_t__cours__40F9A68C");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__payment_t__payme__41EDCAC5");

            entity.HasOne(d => d.User).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__payment_t__user___40058253");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("PK__question__2EC215499BD10942");

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
                .HasConstraintName("FK__question__quiz_i__25518C17");
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.QuizId).HasName("PK__quiz__2D7053EC67043540");

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
                .HasConstraintName("FK__quiz__course_id__1EA48E88");

            entity.HasOne(d => d.Lesson).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.LessonId)
                .HasConstraintName("FK__quiz__lesson_id__1DB06A4F");

            entity.HasOne(d => d.QuizStatusNavigation).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.QuizStatus)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__quiz__quiz_statu__1F98B2C1");
        });

        modelBuilder.Entity<QuizAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptId).HasName("PK__quiz_att__5621F949D80796DA");

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
                .HasConstraintName("FK__quiz_atte__quiz___2EDAF651");

            entity.HasOne(d => d.User).WithMany(p => p.QuizAttempts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__quiz_atte__user___2DE6D218");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__status__3683B531EF75FFC3");

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
            entity.HasKey(e => new { e.UserId, e.AchievementId }).HasName("PK__user_ach__9A7AA5E70AEEA5A1");

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
                .HasConstraintName("FK__user_achi__achie__6CD828CA");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.EnrollmentId)
                .HasConstraintName("FK__user_achi__enrol__6DCC4D03");

            entity.HasOne(d => d.RelatedCourse).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.RelatedCourseId)
                .HasConstraintName("FK__user_achi__relat__6EC0713C");

            entity.HasOne(d => d.User).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__user_achi__user___6BE40491");
        });

        modelBuilder.Entity<UserAnswer>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.QuestionId, e.AttemptId }).HasName("PK__user_ans__730437A22A485D2B");

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
                .HasConstraintName("FK__user_answ__attem__3493CFA7");

            entity.HasOne(d => d.Question).WithMany(p => p.UserAnswers)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__user_answ__quest__339FAB6E");

            entity.HasOne(d => d.SelectedOption).WithMany(p => p.UserAnswers)
                .HasForeignKey(d => d.SelectedOptionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__user_answ__selec__3587F3E0");

            entity.HasOne(d => d.User).WithMany(p => p.UserAnswers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__user_answ__user___32AB8735");
        });

        modelBuilder.Entity<UserProgress>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LessonId }).HasName("PK__user_pro__4FFC28740B0E841A");

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
                .HasConstraintName("FK__user_prog__lesso__4D5F7D71");

            entity.HasOne(d => d.User).WithMany(p => p.UserProgresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__user_prog__user___4C6B5938");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
