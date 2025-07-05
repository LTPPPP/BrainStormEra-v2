namespace BusinessLogicLayer.Constants
{
    /// <summary>
    /// Constants related to Hashids configuration and usage
    /// </summary>
    public static class HashidsConstants
    {
        /// <summary>
        /// Default configuration keys for Hashids
        /// </summary>
        public static class ConfigKeys
        {
            public const string Salt = "Hashids:Salt";
            public const string MinLength = "Hashids:MinLength";
            public const string Alphabet = "Hashids:Alphabet";
        }

        /// <summary>
        /// Default values for Hashids configuration
        /// </summary>
        public static class Defaults
        {
            public const string Salt = "BrainStormEra_Default_Salt_2024";
            public const int MinLength = 8;
            public const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        }

        /// <summary>
        /// Common prefixes for different entity types
        /// </summary>
        public static class Prefixes
        {
            public const string User = "u";
            public const string Course = "c";
            public const string Lesson = "l";
            public const string Chapter = "ch";
            public const string Quiz = "q";
            public const string Question = "qn";
            public const string Certificate = "cert";
            public const string Achievement = "ach";
            public const string Enrollment = "e";
            public const string Payment = "p";
            public const string Notification = "n";
        }

        /// <summary>
        /// Minimum valid ID value
        /// </summary>
        public const int MinValidId = 1;

        /// <summary>
        /// Maximum recommended hash length for URLs
        /// </summary>
        public const int MaxUrlHashLength = 50;
    }
}
