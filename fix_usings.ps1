# PowerShell script to fix all using statements in BusinessLogicLayer

$businessLogicPath = "e:\CN\SU25_K7\PRN222\Final\BrainStormEra\BusinessLogicLayer"

# Get all C# files in BusinessLogicLayer
$files = Get-ChildItem -Path $businessLogicPath -Recurse -Filter "*.cs"

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    $needsUpdate = $false
    
    # Add Microsoft.Extensions.Logging if ILogger is used
    if ($content -match 'ILogger<' -and $content -notmatch 'using Microsoft\.Extensions\.Logging') {
        $content = "using Microsoft.Extensions.Logging;`n" + $content
        $needsUpdate = $true
    }
    
    # Add Microsoft.Extensions.Configuration if IConfiguration is used
    if ($content -match 'IConfiguration' -and $content -notmatch 'using Microsoft\.Extensions\.Configuration') {
        $content = "using Microsoft.Extensions.Configuration;`n" + $content
        $needsUpdate = $true
    }
    
    # Add Microsoft.AspNetCore.Http if HttpContext is used
    if ($content -match 'HttpContext' -and $content -notmatch 'using Microsoft\.AspNetCore\.Http') {
        $content = "using Microsoft.AspNetCore.Http;`n" + $content
        $needsUpdate = $true
    }
    
    # Add Microsoft.AspNetCore.Http if IFormFile is used
    if ($content -match 'IFormFile' -and $content -notmatch 'using Microsoft\.AspNetCore\.Http') {
        $content = "using Microsoft.AspNetCore.Http;`n" + $content
        $needsUpdate = $true
    }
    
    # Add Microsoft.AspNetCore.Hosting if IWebHostEnvironment is used
    if ($content -match 'IWebHostEnvironment' -and $content -notmatch 'using Microsoft\.AspNetCore\.Hosting') {
        $content = "using Microsoft.AspNetCore.Hosting;`n" + $content
        $needsUpdate = $true
    }
    
    if ($needsUpdate) {
        Set-Content $file.FullName -Value $content
        Write-Host "Updated: $($file.FullName)"
    }
}

Write-Host "Completed fixing using statements in BusinessLogicLayer"
