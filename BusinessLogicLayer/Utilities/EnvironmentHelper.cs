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

        // Chatbot Configuration
        public static double GetChatbotTemperature()
        {
            LoadEnvironmentVariables();
            var temp = Environment.GetEnvironmentVariable("CHATBOT_TEMPERATURE");
            return double.TryParse(temp, out var result) ? result : 0.7;
        }

        public static int GetChatbotTopK()
        {
            LoadEnvironmentVariables();
            var topK = Environment.GetEnvironmentVariable("CHATBOT_TOP_K");
            return int.TryParse(topK, out var result) ? result : 40;
        }

        public static double GetChatbotTopP()
        {
            LoadEnvironmentVariables();
            var topP = Environment.GetEnvironmentVariable("CHATBOT_TOP_P");
            return double.TryParse(topP, out var result) ? result : 0.95;
        }

        public static int GetChatbotMaxTokens()
        {
            LoadEnvironmentVariables();
            var maxTokens = Environment.GetEnvironmentVariable("CHATBOT_MAX_TOKENS");
            return int.TryParse(maxTokens, out var result) ? result : 1024;
        }

        public static int GetChatbotCacheHours()
        {
            LoadEnvironmentVariables();
            var cacheHours = Environment.GetEnvironmentVariable("CHATBOT_CACHE_HOURS");
            return int.TryParse(cacheHours, out var result) ? result : 1;
        }

        public static int GetChatbotHistoryLimit()
        {
            LoadEnvironmentVariables();
            var historyLimit = Environment.GetEnvironmentVariable("CHATBOT_HISTORY_LIMIT");
            return int.TryParse(historyLimit, out var result) ? result : 3;
        }

        // System Prompts
        public static string GetChatbotSystemPrompt()
        {
            LoadEnvironmentVariables();
            return Environment.GetEnvironmentVariable("CHATBOT_SYSTEM_PROMPT") ??
                "You are BrainStorm Bot, the intelligent AI assistant of the BrainStormEra learning platform.\n" +
                "Your tasks are:\n" +
                "1. Support students in learning and answering academic questions\n" +
                "2. Provide information about courses, lessons and content on the platform\n" +
                "3. Help explain difficult concepts in an easy-to-understand way\n" +
                "4. Encourage and guide students during their learning process\n\n" +
                "Please respond in English, concisely, friendly and helpfully.\n" +
                "If you don't know the answer, be honest and suggest ways to find out more.";
        }

        public static string GetChatbotErrorMessage()
        {
            LoadEnvironmentVariables();
            return Environment.GetEnvironmentVariable("CHATBOT_ERROR_MESSAGE") ??
                "Sorry, I cannot process your question at this time. Please try again later.";
        }

        public static string GetChatbotApiErrorMessage()
        {
            LoadEnvironmentVariables();
            return Environment.GetEnvironmentVariable("CHATBOT_API_ERROR_MESSAGE") ??
                "Sorry, an error occurred while processing your question. Please try again later.";
        }

        public static string GetChatbotGeneralErrorMessage()
        {
            LoadEnvironmentVariables();
            return Environment.GetEnvironmentVariable("CHATBOT_GENERAL_ERROR_MESSAGE") ??
                "Sorry, an error occurred. Please try again later.";
        }
    }
}