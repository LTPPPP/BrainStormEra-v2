namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IUrlHashService
    {
        /// <summary>
        /// Create hash from real ID
        /// </summary>
        /// <param name="realId">Real ID to hash</param>
        /// <returns>Hash string for use in URL</returns>
        string EncodeId(string realId);

        /// <summary>
        /// Decode hash back to real ID
        /// </summary>
        /// <param name="hash">Hash string from URL</param>
        /// <returns>Real ID</returns>
        string DecodeId(string hash);

        /// <summary>
        /// Create hash for multiple IDs at once
        /// </summary>
        /// <param name="realIds">List of real IDs</param>
        /// <returns>Dictionary mapping real ID to hash</returns>
        Dictionary<string, string> EncodeIds(IEnumerable<string> realIds);

        /// <summary>
        /// Decode multiple hashes at once
        /// </summary>
        /// <param name="hashes">List of hashes</param>
        /// <returns>Dictionary mapping hash to real ID</returns>
        Dictionary<string, string> DecodeIds(IEnumerable<string> hashes);

        /// <summary>
        /// Clear cache (use when refresh is needed)
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Check if string is a hash or not
        /// </summary>
        /// <param name="value">String to check</param>
        /// <returns>True if it's a hash, False if it's a real ID</returns>
        bool IsHash(string value);

        /// <summary>
        /// Get real ID from hash or return itself if already an ID
        /// </summary>
        /// <param name="hashOrId">Hash or ID</param>
        /// <returns>Real ID</returns>
        string GetRealId(string hashOrId);

        /// <summary>
        /// Get hash from real ID or return itself if already a hash
        /// </summary>
        /// <param name="idOrHash">Real ID or hash</param>
        /// <returns>Hash</returns>
        string GetHash(string idOrHash);
    }
}