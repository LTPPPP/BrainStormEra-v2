namespace BusinessLogicLayer.Services.Interfaces
{
    /// <summary>
    /// Interface for LessonTypeSeedService that handles seeding lesson types
    /// </summary>
    public interface ILessonTypeSeedService
    {
        /// <summary>
        /// Seeds lesson types into the database if they don't already exist
        /// </summary>
        /// <returns>Task representing the seeding operation</returns>
        Task SeedLessonTypesAsync();
    }
}