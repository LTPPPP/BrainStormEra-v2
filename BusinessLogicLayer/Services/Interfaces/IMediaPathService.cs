namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IMediaPathService
    {
        /// <summary>
        /// Gets the physical path for storing media files
        /// </summary>
        /// <param name="category">Category of media (avatars, courses, documents, uploads, images)</param>
        /// <returns>Physical path to the directory</returns>
        string GetPhysicalPath(string category);

        /// <summary>
        /// Gets the web-accessible URL for media files
        /// </summary>
        /// <param name="category">Category of media (avatars, courses, documents, uploads, images)</param>
        /// <param name="fileName">Name of the file</param>
        /// <returns>Web-accessible URL</returns>
        string GetWebUrl(string category, string fileName);

        /// <summary>
        /// Ensures the media directory exists
        /// </summary>
        /// <param name="category">Category of media</param>
        void EnsureDirectoryExists(string category);
    }
}