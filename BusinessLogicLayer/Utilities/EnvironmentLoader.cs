using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BusinessLogicLayer.Utilities
{
    public static class EnvironmentLoader
    {
        public static void LoadEnvironmentFile(string filePath = ".env")
        {
            if (!File.Exists(filePath))
            {
                // Try to find .env file in various locations
                var searchPaths = new[]
                {
                    // Current directory
                    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                    
                    // Solution root directory (go up from current executable)
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".env"),
                    
                    // BusinessLogicLayer directory
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "BusinessLogicLayer", ".env"),
                    
                    // Project root (common patterns)
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
                    
                    // Look for solution file to find root
                    FindEnvRelativeToSolution(),
                    
                    // Use assembly location to find root
                    FindEnvUsingAssemblyLocation()
                };

                foreach (var searchPath in searchPaths)
                {
                    if (!string.IsNullOrEmpty(searchPath) && File.Exists(searchPath))
                    {
                        filePath = searchPath;
                        Console.WriteLine($"Found environment file at: {filePath}");
                        break;
                    }
                }

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Environment file not found in any of the expected locations.");
                    Console.WriteLine($"Searched paths:");
                    foreach (var searchPath in searchPaths.Where(p => !string.IsNullOrEmpty(p)))
                    {
                        Console.WriteLine($"  - {searchPath}");
                    }
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

        private static string? FindEnvRelativeToSolution()
        {
            try
            {
                var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var directory = new DirectoryInfo(currentDirectory);

                // Search up the directory tree for a .sln file
                while (directory != null && directory.Parent != null)
                {
                    var solutionFiles = directory.GetFiles("*.sln");
                    if (solutionFiles.Length > 0)
                    {
                        // Found solution directory, look for .env file here
                        var envPath = Path.Combine(directory.FullName, ".env");
                        if (File.Exists(envPath))
                        {
                            return envPath;
                        }
                    }
                    directory = directory.Parent;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string? FindEnvUsingAssemblyLocation()
        {
            try
            {
                // Get the location of the executing assembly
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var directory = new DirectoryInfo(Path.GetDirectoryName(assemblyLocation) ?? "");

                // Search up the directory tree
                while (directory != null && directory.Parent != null)
                {
                    var envPath = Path.Combine(directory.FullName, ".env");
                    if (File.Exists(envPath))
                    {
                        return envPath;
                    }

                    // Also check if this is a solution directory
                    var solutionFiles = directory.GetFiles("*.sln");
                    if (solutionFiles.Length > 0)
                    {
                        break; // Stop at solution level
                    }

                    directory = directory.Parent;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
