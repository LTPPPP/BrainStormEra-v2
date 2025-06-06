using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BrainStormEra_MVC.Services.Repositories
{
    public class CertificateRepository : ICertificateRepository
    {
        private readonly BrainStormEraContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CertificateRepository> _logger;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

        public CertificateRepository(BrainStormEraContext context, IMemoryCache cache, ILogger<CertificateRepository> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<Enrollment>> GetUserCompletedEnrollmentsAsync(string userId)
        {
            var cacheKey = $"UserCompletedEnrollments_{userId}";

            if (_cache.TryGetValue(cacheKey, out List<Enrollment>? cached))
                return cached!;

            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.UserId == userId &&
                           e.EnrollmentStatus == 5 &&
                           e.CertificateIssuedDate != null)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Author)
                .OrderByDescending(e => e.CertificateIssuedDate)
                .ToListAsync();

            _cache.Set(cacheKey, enrollments, CacheExpiration);
            return enrollments;
        }

        public async Task<List<Enrollment>> GetUserCompletedEnrollmentsAsync(string userId, string? search, int page, int pageSize)
        {
            var cacheKey = $"UserCompletedEnrollmentsPaginated_{userId}_{search}_{page}_{pageSize}";

            if (_cache.TryGetValue(cacheKey, out List<Enrollment>? cached))
                return cached!;

            var query = _context.Enrollments
                .AsNoTracking()
                .Where(e => e.UserId == userId &&
                           e.EnrollmentStatus == 5 &&
                           e.CertificateIssuedDate != null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.Course.CourseName.Contains(search) ||
                                       e.Course.Author.FullName!.Contains(search));
            }

            var enrollments = await query
                .Include(e => e.Course)
                    .ThenInclude(c => c.Author)
                .OrderByDescending(e => e.CertificateIssuedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _cache.Set(cacheKey, enrollments, CacheExpiration);
            return enrollments;
        }

        public async Task<int> GetUserCompletedEnrollmentsCountAsync(string userId, string? search)
        {
            var cacheKey = $"UserCompletedEnrollmentsCount_{userId}_{search}";

            if (_cache.TryGetValue(cacheKey, out int cached))
                return cached;

            var query = _context.Enrollments
                .AsNoTracking()
                .Where(e => e.UserId == userId &&
                           e.EnrollmentStatus == 5 &&
                           e.CertificateIssuedDate != null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.Course.CourseName.Contains(search) ||
                                       e.Course.Author.FullName!.Contains(search));
            }

            var count = await query.CountAsync();
            _cache.Set(cacheKey, count, CacheExpiration);
            return count;
        }

        public async Task<Enrollment?> GetCertificateDataAsync(string userId, string courseId)
        {
            var cacheKey = $"CertificateData_{userId}_{courseId}";

            if (_cache.TryGetValue(cacheKey, out Enrollment? cached))
                return cached;

            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.UserId == userId &&
                           e.CourseId == courseId &&
                           e.EnrollmentStatus == 5 &&
                           e.CertificateIssuedDate != null)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Author)
                .Include(e => e.User)
                .FirstOrDefaultAsync();

            if (enrollment != null)
                _cache.Set(cacheKey, enrollment, CacheExpiration);

            return enrollment;
        }

        public async Task<bool> HasValidCertificateAsync(string userId, string courseId)
        {
            var cacheKey = $"HasCertificate_{userId}_{courseId}";

            if (_cache.TryGetValue(cacheKey, out bool cached))
                return cached;

            var hasCertificate = await _context.Enrollments
                .AsNoTracking()
                .AnyAsync(e => e.UserId == userId &&
                              e.CourseId == courseId &&
                              e.EnrollmentStatus == 5 &&
                              e.CertificateIssuedDate != null);

            _cache.Set(cacheKey, hasCertificate, CacheExpiration);
            return hasCertificate;
        }

        public async Task<Certificate?> GetCertificateByCodeAsync(string certificateCode)
        {
            return await _context.Certificates
                .AsNoTracking()
                .Include(c => c.Course)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CertificateCode == certificateCode);
        }

        public async Task<int> GetUserCertificateCountAsync(string userId)
        {
            var cacheKey = $"UserCertificateCount_{userId}";

            if (_cache.TryGetValue(cacheKey, out int cached))
                return cached;

            var count = await _context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.UserId == userId &&
                               e.EnrollmentStatus == 5 &&
                               e.CertificateIssuedDate != null);

            _cache.Set(cacheKey, count, CacheExpiration);
            return count;
        }
    }
}
