using System;
using System.IO;

namespace BusinessLogicLayer.Utilities
{
    public static class EnvironmentHelper
    {
        private static bool _isLoaded = false;

        public static void LoadEnvironmentVariables()
        {
            if (_isLoaded) return;

            try
            {
                var envFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");

                // If .env doesn't exist in the current directory, try the project root
                if (!File.Exists(envFilePath))
                {
                    var projectRoot = GetProjectRoot();
                    envFilePath = Path.Combine(projectRoot, "BusinessLogicLayer", ".env");
                }

                if (File.Exists(envFilePath))
                {
                    var lines = File.ReadAllLines(envFilePath);

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                            continue;

                        var parts = line.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();

                            // Remove quotes if present
                            if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                                (value.StartsWith("'") && value.EndsWith("'")))
                            {
                                value = value.Substring(1, value.Length - 2);
                            }

                            Environment.SetEnvironmentVariable(key, value);
                        }
                    }
                }

                _isLoaded = true;
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - fallback to other configuration sources
                Console.WriteLine($"Warning: Could not load .env file: {ex.Message}");
            }
        }

        private static string GetProjectRoot()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var directory = new DirectoryInfo(currentDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "BrainStormEra.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? currentDirectory;
        }

        public static string GetChatbotApiKey()
        {
            LoadEnvironmentVariables();
            return Environment.GetEnvironmentVariable("CHATBOT_API_KEY") ?? string.Empty;
        }

        public static string GetChatbotApiUrl()
        {
            LoadEnvironmentVariables();
            return Environment.GetEnvironmentVariable("CHATBOT_API_URL") ?? string.Empty;
        }
    }
}