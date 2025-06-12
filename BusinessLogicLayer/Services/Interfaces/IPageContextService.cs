namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IPageContextService
    {
        Task<string> GetPageContextAsync(string path, string? courseId = null, string? chapterId = null, string? lessonId = null);
        Task<string> GetCourseContextAsync(string courseId);
        Task<string> GetChapterContextAsync(string chapterId);
        Task<string> GetLessonContextAsync(string lessonId);
    }
}







