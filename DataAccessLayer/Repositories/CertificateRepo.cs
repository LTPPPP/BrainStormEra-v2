using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataAccessLayer.Repositories
{
    public class CertificateRepo : BaseRepo<Certificate>, ICertificateRepo
    {
        private readonly ILogger<CertificateRepo>? _logger;

        public CertificateRepo(BrainStormEraContext context, ILogger<CertificateRepo>? logger = null)
            : base(context)
        {
            _logger = logger;
        }

        // Certificate query methods
        public async Task<List<Certificate>> GetUserCertificatesAsync(string userId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Course)
                    .Include(c => c.User)
                    .Include(c => c.Enrollment)
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.CertificateCreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user certificates: {UserId}", userId);
                throw;
            }
        }

        public async Task<Certificate?> GetCertificateByCourseAsync(string userId, string courseId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Course)
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting certificate by course: {UserId}, {CourseId}", userId, courseId);
                throw;
            }
        }

        public async Task<List<Certificate>> GetCertificatesByCourseAsync(string courseId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.User)
                    .Include(c => c.Course)
                    .Where(c => c.CourseId == courseId)
                    .OrderByDescending(c => c.IssueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting certificates by course: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<Certificate?> GetCertificateWithDetailsAsync(string certificateId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Course)
                        .ThenInclude(course => course.Author)
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CertificateId == certificateId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting certificate with details: {CertificateId}", certificateId);
                throw;
            }
        }

        // Certificate generation and management
        public async Task<string> GenerateCertificateAsync(string userId, string courseId)
        {
            try
            {
                // Check if certificate already exists
                var existingCertificate = await GetCertificateByCourseAsync(userId, courseId);
                if (existingCertificate != null)
                    return existingCertificate.CertificateId;

                // Get enrollment ID
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

                if (enrollment == null)
                    throw new InvalidOperationException("User must be enrolled in course to receive certificate");

                var certificate = new Certificate
                {
                    CertificateId = Guid.NewGuid().ToString(),
                    EnrollmentId = enrollment.EnrollmentId,
                    UserId = userId,
                    CourseId = courseId,
                    CertificateCode = GenerateVerificationCode(),
                    CertificateName = $"Certificate of Completion",
                    IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    IsValid = true,
                    CertificateCreatedAt = DateTime.UtcNow
                };

                await AddAsync(certificate);
                await SaveChangesAsync();
                return certificate.CertificateId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating certificate");
                throw;
            }
        }

        public async Task<bool> UpdateCertificateAsync(Certificate certificate)
        {
            try
            {
                await UpdateAsync(certificate);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating certificate");
                throw;
            }
        }

        public async Task<bool> DeleteCertificateAsync(string certificateId, string userId)
        {
            try
            {
                var certificate = await _dbSet
                    .FirstOrDefaultAsync(c => c.CertificateId == certificateId && c.UserId == userId);

                if (certificate == null)
                    return false;

                await DeleteAsync(certificate);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting certificate");
                throw;
            }
        }

        public async Task<bool> RegenerateCertificateAsync(string certificateId)
        {
            try
            {
                var certificate = await GetByIdAsync(certificateId);
                if (certificate == null)
                    return false;

                certificate.IssueDate = DateOnly.FromDateTime(DateTime.UtcNow);
                certificate.CertificateCode = GenerateVerificationCode();

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error regenerating certificate");
                throw;
            }
        }

        // Certificate validation and verification
        public async Task<Certificate?> VerifyCertificateAsync(string certificateId, string verificationCode)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Course)
                    .Include(c => c.User)
                    .Include(c => c.Enrollment)
                    .FirstOrDefaultAsync(c => c.CertificateId == certificateId && c.CertificateCode == verificationCode);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error verifying certificate");
                throw;
            }
        }

        public async Task<bool> IsCertificateValidAsync(string certificateId)
        {
            try
            {
                var certificate = await GetByIdAsync(certificateId);
                if (certificate == null)
                    return false;

                // Add validation logic here (e.g., check expiry date, course completion, etc.)
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error validating certificate");
                throw;
            }
        }

        public async Task<bool> HasUserEarnedCertificateAsync(string userId, string courseId)
        {
            try
            {
                return await _dbSet
                    .AnyAsync(c => c.UserId == userId && c.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if user earned certificate");
                throw;
            }
        }

        public async Task<string> GenerateVerificationCodeAsync(string certificateId)
        {
            try
            {
                var certificate = await GetByIdAsync(certificateId);
                if (certificate == null)
                    return string.Empty;

                var verificationCode = GenerateVerificationCode();
                certificate.CertificateCode = verificationCode;

                await SaveChangesAsync();
                return verificationCode;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating verification code");
                throw;
            }
        }

        // Certificate download and export
        public async Task<byte[]?> GetCertificatePdfAsync(string certificateId)
        {
            try
            {
                var certificate = await GetByIdAsync(certificateId);
                if (certificate == null || string.IsNullOrEmpty(certificate.CertificateUrl))
                    return null;

                // This would read the PDF file from the file system
                // For now, returning null as placeholder
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting certificate PDF");
                throw;
            }
        }

        public async Task<bool> UpdateCertificateFilePathAsync(string certificateId, string filePath)
        {
            try
            {
                var certificate = await GetByIdAsync(certificateId);
                if (certificate == null)
                    return false;

                certificate.CertificateUrl = filePath;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating certificate file path");
                throw;
            }
        }

        public async Task<string?> GetCertificateFilePathAsync(string certificateId)
        {
            try
            {
                var certificate = await GetByIdAsync(certificateId);
                return certificate?.CertificateUrl;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting certificate file path");
                throw;
            }
        }

        // Certificate statistics
        public async Task<int> GetUserCertificateCountAsync(string userId)
        {
            try
            {
                return await _dbSet
                    .CountAsync(c => c.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user certificate count");
                throw;
            }
        }

        public async Task<int> GetCourseCertificateCountAsync(string courseId)
        {
            try
            {
                return await _dbSet
                    .CountAsync(c => c.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting course certificate count");
                throw;
            }
        }

        public async Task<List<Certificate>> GetRecentCertificatesAsync(int count = 10)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Course)
                    .Include(c => c.User)
                    .OrderByDescending(c => c.IssueDate)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting recent certificates");
                throw;
            }
        }

        public async Task<decimal> GetCertificateCompletionRateAsync(string courseId)
        {
            try
            {
                var enrolledCount = await _context.Enrollments
                    .CountAsync(e => e.CourseId == courseId);

                if (enrolledCount == 0) return 0;

                var certificateCount = await GetCourseCertificateCountAsync(courseId);
                return Math.Round((decimal)certificateCount / enrolledCount * 100, 2);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting certificate completion rate");
                throw;
            }
        }

        // Missing methods implementation
        public async Task<Enrollment?> GetCertificateDataAsync(string userId, string courseId)
        {
            try
            {
                return await _context.Enrollments
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Author)
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId && e.CertificateIssuedDate.HasValue);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting certificate data");
                throw;
            }
        }

        public async Task<bool> HasValidCertificateAsync(string userId, string courseId)
        {
            try
            {
                return await _context.Enrollments
                    .AnyAsync(e => e.UserId == userId && e.CourseId == courseId && e.CertificateIssuedDate.HasValue);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking valid certificate");
                throw;
            }
        }

        public async Task<List<Enrollment>> GetUserCompletedEnrollmentsAsync(string userId, string? search, int page, int pageSize)
        {
            try
            {
                var query = _context.Enrollments
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Author)
                    .Include(e => e.User)
                    .Where(e => e.UserId == userId && e.CertificateIssuedDate.HasValue);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(e => e.Course.CourseName.Contains(search));
                }

                return await query
                    .OrderByDescending(e => e.CertificateIssuedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user completed enrollments");
                throw;
            }
        }

        public async Task<int> GetUserCompletedEnrollmentsCountAsync(string userId, string? search)
        {
            try
            {
                var query = _context.Enrollments
                    .Include(e => e.Course)
                    .Where(e => e.UserId == userId && e.CertificateIssuedDate.HasValue);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(e => e.Course.CourseName.Contains(search));
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user completed enrollments count");
                throw;
            }
        }

        // Helper method to generate verification code
        private string GenerateVerificationCode()
        {
            return Guid.NewGuid().ToString("N")[..8].ToUpper();
        }

        // Basic implementations for remaining interface methods
        public async Task<bool> UpdateCertificateTemplateAsync(string courseId, string templatePath) => true;
        public async Task<string?> GetCertificateTemplateAsync(string courseId) => null;
        public async Task<bool> CustomizeCertificateAsync(string certificateId, Dictionary<string, string> customFields) => true;
        public async Task<bool> ShareCertificateAsync(string certificateId, string platform) => true;
        public async Task<List<Certificate>> GetPublicCertificatesAsync(string userId) => await GetUserCertificatesAsync(userId);
        public async Task<bool> UpdateCertificateVisibilityAsync(string certificateId, bool isPublic) => true;
        public async Task<List<Certificate>> GetExpiringCertificatesAsync(int daysBeforeExpiry = 30) => new();
        public async Task<bool> RenewCertificateAsync(string certificateId) => await RegenerateCertificateAsync(certificateId);
        public async Task<bool> UpdateCertificateExpiryAsync(string certificateId, DateTime? expiryDate) => true;
        public async Task<List<Certificate>> GetExpiredCertificatesAsync() => new();

        public async Task<List<Certificate>> SearchCertificatesAsync(string searchTerm, string? userId = null)
        {
            try
            {
                var query = _dbSet
                    .Include(c => c.Course)
                    .Include(c => c.User)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(c => c.UserId == userId);
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(c => c.Course.CourseName.Contains(searchTerm) ||
                                           c.User.Username.Contains(searchTerm));
                }

                return await query
                    .OrderByDescending(c => c.IssueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching certificates");
                throw;
            }
        }

        public async Task<List<Certificate>> GetCertificatesByDateRangeAsync(DateTime startDate, DateTime endDate, string? userId = null)
        {
            try
            {
                var query = _dbSet
                    .Include(c => c.Course)
                    .Include(c => c.User)
                    .Where(c => c.IssueDate >= DateOnly.FromDateTime(startDate) && c.IssueDate <= DateOnly.FromDateTime(endDate));

                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(c => c.UserId == userId);
                }

                return await query
                    .OrderByDescending(c => c.IssueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting certificates by date range");
                throw;
            }
        }

        public async Task<List<Certificate>> GetCertificatesByInstructorAsync(string instructorId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Course)
                    .Include(c => c.User)
                    .Where(c => c.Course.AuthorId == instructorId)
                    .OrderByDescending(c => c.IssueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting certificates by instructor");
                throw;
            }
        }
    }
}
