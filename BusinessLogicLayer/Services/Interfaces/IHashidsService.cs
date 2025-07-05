namespace BusinessLogicLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface for encoding and decoding IDs using Hashids
    /// </summary>
    public interface IHashidsService
    {
        /// <summary>
        /// Encode an integer ID to a hash string
        /// </summary>
        /// <param name="id">The integer ID to encode</param>
        /// <returns>Encoded hash string</returns>
        string Encode(int id);

        /// <summary>
        /// Encode multiple integer IDs to a hash string
        /// </summary>
        /// <param name="ids">Array of integer IDs to encode</param>
        /// <returns>Encoded hash string</returns>
        string Encode(params int[] ids);

        /// <summary>
        /// Decode a hash string to an array of integer IDs
        /// </summary>
        /// <param name="hash">The hash string to decode</param>
        /// <returns>Array of decoded integer IDs</returns>
        int[] Decode(string hash);

        /// <summary>
        /// Decode a hash string to a single integer ID
        /// </summary>
        /// <param name="hash">The hash string to decode</param>
        /// <returns>Decoded integer ID, or 0 if invalid</returns>
        int DecodeSingle(string hash);

        /// <summary>
        /// Check if a hash string is valid
        /// </summary>
        /// <param name="hash">The hash string to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        bool IsValidHash(string hash);

        /// <summary>
        /// Encode a long ID to a hash string
        /// </summary>
        /// <param name="id">The long ID to encode</param>
        /// <returns>Encoded hash string</returns>
        string EncodeLong(long id);

        /// <summary>
        /// Decode a hash string to a single long ID
        /// </summary>
        /// <param name="hash">The hash string to decode</param>
        /// <returns>Decoded long ID, or 0 if invalid</returns>
        long DecodeLong(string hash);
    }
}
