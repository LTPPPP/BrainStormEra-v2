using System;
using System.IO;

namespace BusinessLogicLayer.Utilities
{
    public static class EnvironmentLoader
    {
        public static void LoadEnvironmentFile(string filePath = ".env")
        {
            if (!File.Exists(filePath))
            {
                // Try to find .env file in BusinessLogicLayer directory
                var businessLogicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "BusinessLogicLayer", ".env");
                if (File.Exists(businessLogicPath))
                {
                    filePath = businessLogicPath;
                }
                else
                {
                    Console.WriteLine($"Environment file not found: {filePath}");
                    return;
                }
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    // Skip empty lines and comments
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
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

                        // Only set if not already exists (allows override)
                        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                        {
                            Environment.SetEnvironmentVariable(key, value);
                        }
                    }
                }
                Console.WriteLine($"Environment file loaded successfully: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading environment file: {ex.Message}");
            }
        }
    }
}
