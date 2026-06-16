<#
.SYNOPSIS
    Copies a directory and its contents using Robocopy with predefined options.

.DESCRIPTION
    Copies files and subdirectories from a source directory to a destination directory using Robocopy.
    Creates the destination directory if it doesn't exist. Uses Robocopy with options for subdirectories,
    retries, and quiet output. Handles Robocopy exit codes and throws an error if the copy operation fails.

.PARAMETER Source
    The path to the source directory to copy from. Must be a valid existing directory path.

.PARAMETER Destination
    The path to the destination directory to copy to. Will be created if it doesn't exist.

.EXAMPLE
    Copy-DirWithRobocopy -Source "C:\SourceFolder" -Destination "C:\DestinationFolder"
    Copies all files and subdirectories from C:\SourceFolder to C:\DestinationFolder.

.NOTES
    - Uses Robocopy with /E (subdirs including empty), /R:3 (3 retries), /W:5 (5 second wait), and quiet switches
    - Robocopy exit codes 0-7 indicate success, 8+ indicate failure
    - Will create the destination directory if it doesn't exist
    - Warns and skips if source directory doesn't exist
#>
function Copy-DirWithRobocopy {
    param(
        [Parameter(Mandatory=$true)][string]$Source,
        [Parameter(Mandatory=$true)][string]$Destination
    )

    if (-not (Test-Path -Path $Source)) {
        Write-Warning "Source not found, skipping: $Source"
        return
    }

    if (-not (Test-Path -Path $Destination)) {
        New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    }

    # /E = subdirs incl empty, /R /W = retries, and quiet switches
    $null = robocopy $Source $Destination *.* /E /R:3 /W:5 /NFL /NDL /NJH /NJS /NC /NS /NP
    $rc = $LASTEXITCODE

    # Robocopy exit codes: 0–7 => success; >=8 => failure
    if ($rc -ge 8) {
        throw "RoboCopy failed copying `"$Source`" to `"$Destination`" with exit code $rc"
    }
}

<#
.SYNOPSIS
    Stops any running instances of the Skyline Device Simulator (QADeviceSimulator).

.DESCRIPTION
    Checks for running processes named 'QADeviceSimulator' and forcefully terminates them.
    Provides console output about the number of processes found and stopped.
    Waits 2 seconds after stopping processes to allow for cleanup.

.EXAMPLE
    Stop-SkylineDeviceSimulatorIfRunning
    Stops all running instances of QADeviceSimulator.exe if any are found.

.NOTES
    - Uses Get-Process with ErrorAction SilentlyContinue to avoid errors if no processes are found
    - Uses Stop-Process with -Force to ensure processes are terminated
    - Exits with code 1 if unable to stop processes
    - Includes a 2-second sleep after stopping processes
#>
function Stop-SkylineDeviceSimulatorIfRunning {
    $procs = Get-Process -Name 'QADeviceSimulator' -ErrorAction SilentlyContinue
    if ($procs) {
        Write-Host "Stopping Skyline Device Simulator (found $($procs.Count))..." -ForegroundColor Yellow
        try {
            $procs | Stop-Process -Force -ErrorAction Stop
            Start-Sleep -Seconds 2
        }
        catch {
            Write-Error "Failed to stop Skyline Device Simulator: $($_.Exception.Message)"
            exit 1
        }
    }
}

<#
.SYNOPSIS
    Stops any running instances of SLAutomation.exe.

.DESCRIPTION
    Checks for running processes named 'SLAutomation' and forcefully terminates them.
    Provides console output about the number of processes found and stopped.
    Waits 2 seconds after stopping processes to allow for cleanup.

.EXAMPLE
    Stop-SLAutomationIfRunning
    Stops all running instances of SLAutomation.exe if any are found.

.NOTES
    - Uses Get-Process with ErrorAction SilentlyContinue to avoid errors if no processes are found
    - Uses Stop-Process with -Force to ensure processes are terminated
    - Exits with code 1 if unable to stop processes
    - Includes a 2-second sleep after stopping processes
#>
function Stop-SLAutomationIfRunning {
    $procs = Get-Process -Name 'SLAutomation' -ErrorAction SilentlyContinue
    if ($procs) {
        Write-Host "Stopping SLAutomation.exe (found $($procs.Count))..." -ForegroundColor Yellow
        try {
            $procs | Stop-Process -Force -ErrorAction Stop
            Start-Sleep -Seconds 2
        }
        catch {
            Write-Error "Failed to stop SLAutomation.exe: $($_.Exception.Message)"
            exit 1
        }
    }
}

<#
.SYNOPSIS
    Truncates a string to a specified maximum length with an ellipsis indicator.

.DESCRIPTION
    Limits the length of a string by truncating it to the specified maximum number of characters.
    If the string exceeds the maximum length, it is cut off and "...(truncated)" is appended.
    Returns the original string unchanged if it's within the character limit or if it's null/empty.

.PARAMETER stringToLimit
    The string to be limited. Can be null or empty.

.PARAMETER maxCharacters
    The maximum number of characters allowed before truncation. Default is 2000.

.EXAMPLE
    Limit-String -stringToLimit "This is a very long string" -maxCharacters 10
    Returns: "This is a ...(truncated)"

.EXAMPLE
    Limit-String "Short string"
    Returns: "Short string" (unchanged because it's under the default 2000 character limit)

.NOTES
    - Returns the original string if it's null, empty, or within the character limit
    - Default maximum is 2000 characters
    - Truncated strings have "...(truncated)" appended
#>
function Limit-String {
    param(
        [Parameter(Mandatory=$true)][string]$stringToLimit,
        [Parameter(Mandatory=$false)][int]$maxCharacters = 2000
    )
    
    if ([string]::IsNullOrEmpty($stringToLimit)) { return $stringToLimit }
    if ($stringToLimit.Length -le $maxCharacters) { return $stringToLimit }
    return $stringToLimit.Substring(0, $maxCharacters) + "...(truncated)"
}
