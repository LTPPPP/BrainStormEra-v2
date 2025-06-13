-- Script to mark some courses as featured for testing recommendations
-- This will help populate the "Recommended For You" section

USE [BrainStormEra];
GO

-- Mark the first 5 active courses as featured
UPDATE TOP(5) Courses 
SET IsFeatured = 1, 
    CourseUpdatedAt = GETDATE()
WHERE CourseStatus = 1 
  AND (IsFeatured IS NULL OR IsFeatured = 0);

-- Show which courses are now featured
SELECT 
    CourseId,
    CourseName,
    AuthorId,
    Price,
    IsFeatured,
    CourseStatus,
    CourseCreatedAt
FROM Courses 
WHERE IsFeatured = 1 
  AND CourseStatus = 1
ORDER BY CourseCreatedAt DESC;

-- Check total count
SELECT 
    COUNT(*) as TotalCourses,
    SUM(CASE WHEN IsFeatured = 1 THEN 1 ELSE 0 END) as FeaturedCourses,
    SUM(CASE WHEN CourseStatus = 1 THEN 1 ELSE 0 END) as ActiveCourses
FROM Courses;
