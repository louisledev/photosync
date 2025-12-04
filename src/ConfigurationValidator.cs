using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Extensions.Configuration;

namespace PhotoSync
{
    /// <summary>
    /// Helper class to validate OneDrive configuration before running the sync
    /// Run this to ensure your credentials and folder paths are correct
    /// </summary>
    public class ConfigurationValidator
    {
        public static async Task<bool> ValidateAllConfigurationsAsync(IConfiguration configuration)
        {
            Console.WriteLine("=== Photo Sync Configuration Validator ===\n");
            
            bool allValid = true;
            
            // Validate OneDrive 1
            Console.WriteLine("Validating OneDrive Account 1...");
            var oneDrive1Valid = await ValidateOneDriveConfigAsync(
                configuration["OneDrive1:ClientId"],
                configuration["OneDrive1:TenantId"],
                configuration["OneDrive1:ClientSecret"],
                configuration["OneDrive1:SourceFolder"],
                "OneDrive1"
            );
            allValid = allValid && oneDrive1Valid;
            
            // Validate OneDrive 2
            Console.WriteLine("\nValidating OneDrive Account 2...");
            var oneDrive2Valid = await ValidateOneDriveConfigAsync(
                configuration["OneDrive2:ClientId"],
                configuration["OneDrive2:TenantId"],
                configuration["OneDrive2:ClientSecret"],
                configuration["OneDrive2:SourceFolder"],
                "OneDrive2"
            );
            allValid = allValid && oneDrive2Valid;
            
            // Validate Destination
            Console.WriteLine("\nValidating Destination OneDrive Account...");
            var destinationValid = await ValidateOneDriveConfigAsync(
                configuration["OneDriveDestination:ClientId"],
                configuration["OneDriveDestination:TenantId"],
                configuration["OneDriveDestination:ClientSecret"],
                configuration["OneDriveDestination:DestinationFolder"],
                "OneDriveDestination"
            );
            allValid = allValid && destinationValid;
            
            Console.WriteLine("\n===========================================");
            if (allValid)
            {
                Console.WriteLine("✓ All configurations are valid!");
                Console.WriteLine("You're ready to run the photo sync.");
            }
            else
            {
                Console.WriteLine("✗ Some configurations are invalid.");
                Console.WriteLine("Please fix the issues above before running.");
            }
            Console.WriteLine("===========================================\n");
            
            return allValid;
        }
        
        private static async Task<bool> ValidateOneDriveConfigAsync(
            string clientId, 
            string tenantId, 
            string clientSecret, 
            string folderPath,
            string accountName)
        {
            // Check if values are set
            if (string.IsNullOrEmpty(clientId) || clientId.Contains("your-"))
            {
                Console.WriteLine($"  ✗ ClientId not configured");
                return false;
            }
            
            if (string.IsNullOrEmpty(tenantId) || tenantId.Contains("your-"))
            {
                Console.WriteLine($"  ✗ TenantId not configured");
                return false;
            }
            
            if (string.IsNullOrEmpty(clientSecret) || clientSecret.Contains("your-"))
            {
                Console.WriteLine($"  ✗ ClientSecret not configured");
                return false;
            }
            
            if (string.IsNullOrEmpty(folderPath))
            {
                Console.WriteLine($"  ✗ Folder path not configured");
                return false;
            }
            
            // Validate folder path format
            if (folderPath.StartsWith("/"))
            {
                Console.WriteLine($"  ⚠ Warning: Folder path starts with '/'. Should be relative (e.g., 'Pictures/Folder')");
            }
            
            if (folderPath.Contains("\\"))
            {
                Console.WriteLine($"  ✗ Folder path uses backslashes. Use forward slashes (e.g., 'Pictures/Folder')");
                return false;
            }
            
            // Try to authenticate
            try
            {
                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                var graphClient = new GraphServiceClient(credential);
                
                // Try to access the user's drive
                var drive = await graphClient.Me.Drive.GetAsync();
                Console.WriteLine($"  ✓ Authentication successful");
                Console.WriteLine($"    Drive Owner: {drive?.Owner?.User?.DisplayName ?? "Unknown"}");
                
                // Try to access the specified folder (Graph v5 syntax)
                // In v5, use Drives["me"] instead of Me.Drive
                try
                {
                    var folder = await graphClient.Drives["me"]
                        .Root
                        .ItemWithPath(folderPath)
                        .GetAsync();

                    Console.WriteLine($"  ✓ Folder found: {folderPath}");
                    Console.WriteLine($"    Folder name: {folder?.Name}");

                    // Count items in folder
                    var children = await graphClient.Drives["me"]
                        .Root
                        .ItemWithPath(folderPath)
                        .Children
                        .GetAsync();
                    
                    var photoCount = children?.Value?
                        .Count(item => item.File != null && 
                               (item.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                item.Name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                item.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                item.Name.EndsWith(".heic", StringComparison.OrdinalIgnoreCase))) ?? 0;
                    
                    Console.WriteLine($"    Photos found: {photoCount}");
                    
                    if (photoCount == 0)
                    {
                        Console.WriteLine($"  ⚠ Warning: No photos found in folder");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Could not access folder: {folderPath}");
                    Console.WriteLine($"    Error: {ex.Message}");
                    Console.WriteLine($"    Tip: Check that the folder exists and path is correct");
                    return false;
                }
                
                return true;
            }
            catch (Azure.Identity.AuthenticationFailedException ex)
            {
                Console.WriteLine($"  ✗ Authentication failed");
                Console.WriteLine($"    Error: {ex.Message}");
                Console.WriteLine($"    Tip: Check ClientId, TenantId, and ClientSecret are correct");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Unexpected error: {ex.Message}");
                return false;
            }
        }
    }
}
