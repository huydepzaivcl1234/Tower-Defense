param(
    [string]$ProjectPath = "C:\Users\huyancut\game",
    [string]$RemoteBranch = "main",
    [int]$AddTimeoutSeconds = 180,
    [int]$CommitTimeoutSeconds = 120,
    [int]$PushTimeoutSeconds = 600
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$Message) {
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Invoke-GitWithTimeout {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][int]$TimeoutSeconds,
        [string]$Label = "git"
    )

    $stdoutFile = [System.IO.Path]::GetTempFileName()
    $stderrFile = [System.IO.Path]::GetTempFileName()

    try {
        $argumentLine = ($Arguments | ForEach-Object {
            if ($_ -match '[\s\"]') { '"' + ($_ -replace '"', '\"') + '"' } else { $_ }
        }) -join ' '

        $startInfo = New-Object System.Diagnostics.ProcessStartInfo
        $startInfo.FileName = "git.exe"
        $startInfo.Arguments = $argumentLine
        $startInfo.WorkingDirectory = $ProjectPath
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $startInfo.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0"
        $startInfo.EnvironmentVariables["GCM_INTERACTIVE"] = "Never"

        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $startInfo

        if (-not $process.Start()) {
            throw "Khong the khoi dong git.exe cho buoc: $Label"
        }

        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()

        if (-not $process.WaitForExit([Math]::Max(1, $TimeoutSeconds) * 1000)) {
            try { $process.Kill() } catch {}
            try { $process.WaitForExit(3000) | Out-Null } catch {}
            throw "$Label bi timeout sau $TimeoutSeconds giay. Da dung process Git thay vi de no treo vo han."
        }

        $stdout = $stdoutTask.Result
        $stderr = $stderrTask.Result

        if (-not [string]::IsNullOrWhiteSpace($stdout)) {
            Write-Host $stdout.TrimEnd()
        }
        if (-not [string]::IsNullOrWhiteSpace($stderr)) {
            if ($process.ExitCode -eq 0) {
                Write-Host $stderr.TrimEnd() -ForegroundColor DarkYellow
            } else {
                Write-Host $stderr.TrimEnd() -ForegroundColor Red
            }
        }

        return [PSCustomObject]@{
            ExitCode = $process.ExitCode
            StdOut = $stdout
            StdErr = $stderr
        }
    }
    finally {
        Remove-Item -LiteralPath $stdoutFile, $stderrFile -Force -ErrorAction SilentlyContinue
    }
}

function Assert-GitSuccess($Result, [string]$Label) {
    if ($null -eq $Result -or $Result.ExitCode -ne 0) {
        $detail = if ($null -ne $Result -and -not [string]::IsNullOrWhiteSpace($Result.StdErr)) { $Result.StdErr.Trim() } else { "Unknown Git error." }
        throw "$Label that bai.`n$detail"
    }
}

Write-Host "=== Tower Defense Quick Push (Safe) ===" -ForegroundColor Green
Write-Host "Project: $ProjectPath"
Write-Host "Remote branch: $RemoteBranch"

if (-not (Test-Path -LiteralPath $ProjectPath)) {
    throw "Khong tim thay project: $ProjectPath"
}

Set-Location -LiteralPath $ProjectPath

if (-not (Test-Path -LiteralPath ".git")) {
    throw "Thu muc nay khong phai Git repository: $ProjectPath"
}

$indexLock = Join-Path $ProjectPath ".git\index.lock"
if (Test-Path -LiteralPath $indexLock) {
    throw "Git dang co index.lock: $indexLock`nCo the mot Git process khac dang chay hoac lan truoc bi crash. Dong cac Git process dang chay roi thu lai. Khong tu dong xoa lock de tranh mat du lieu."
}

# Prevent two copies of Quick Push from running at the same time.
$mutexName = "Global\TowerDefense_QuickPush_" + ([Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($ProjectPath))).Replace("=", "").Replace("/", "_").Replace("+", "-")
$mutex = New-Object System.Threading.Mutex($false, $mutexName)
$hasMutex = $false

try {
    try {
        $hasMutex = $mutex.WaitOne(0, $false)
    }
    catch [System.Threading.AbandonedMutexException] {
        $hasMutex = $true
    }

    if (-not $hasMutex) {
        throw "Quick Push dang chay o cua so/process khac. Cho lan push hien tai ket thuc truoc."
    }

    Write-Step "Check branch"
    $branchResult = Invoke-GitWithTimeout -Arguments @("branch", "--show-current") -TimeoutSeconds 15 -Label "git branch"
    Assert-GitSuccess $branchResult "Kiem tra branch"
    $currentBranch = $branchResult.StdOut.Trim()
    Write-Host "Current branch: $currentBranch" -ForegroundColor Yellow

    if ($currentBranch -ne "main") {
        throw "Quick Push chi duoc push khi local branch la main. Hien tai: '$currentBranch'."
    }

    $gitignorePath = Join-Path $ProjectPath ".gitignore"
    $marker = "# === ChatGPT Unity generated-cache ignore ==="
    $ignoreBlock = @'
# === ChatGPT Unity generated-cache ignore ===
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
UserSettings/

.vs/
.vscode/
.idea/

*.csproj
*.sln
*.slnx
*.user
*.userprefs

Assets/**/*.cs.backup
Assets/**/*.backup
# === End ChatGPT Unity generated-cache ignore ===
'@

    if (-not (Test-Path -LiteralPath $gitignorePath)) {
        Set-Content -LiteralPath $gitignorePath -Value $ignoreBlock -Encoding UTF8
    }
    else {
        $existingIgnore = Get-Content -LiteralPath $gitignorePath -Raw
        if ($existingIgnore -notmatch [regex]::Escape($marker)) {
            Add-Content -LiteralPath $gitignorePath -Value "`r`n$ignoreBlock" -Encoding UTF8
        }
    }

    Write-Step "Stage Unity project files"
    $paths = @("Assets", "Packages", "ProjectSettings", "Tools", ".gitignore")
    foreach ($p in $paths) {
        if (Test-Path -LiteralPath $p) {
            $addResult = Invoke-GitWithTimeout -Arguments @("add", "--", $p) -TimeoutSeconds $AddTimeoutSeconds -Label "git add $p"
            Assert-GitSuccess $addResult "git add $p"
        }
    }

    Write-Step "Status"
    $statusResult = Invoke-GitWithTimeout -Arguments @("status", "--short") -TimeoutSeconds 30 -Label "git status"
    Assert-GitSuccess $statusResult "git status"

    $diffResult = Invoke-GitWithTimeout -Arguments @("diff", "--cached", "--quiet") -TimeoutSeconds 30 -Label "git diff --cached"
    $hasChanges = ($diffResult.ExitCode -eq 1)
    if ($diffResult.ExitCode -ne 0 -and $diffResult.ExitCode -ne 1) {
        throw "Khong kiem tra duoc staged changes.`n$($diffResult.StdErr)"
    }

    if ($hasChanges) {
        Write-Step "Commit"
        $stamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        $commitResult = Invoke-GitWithTimeout -Arguments @("commit", "-m", "Quick push Unity project $stamp") -TimeoutSeconds $CommitTimeoutSeconds -Label "git commit"
        Assert-GitSuccess $commitResult "git commit"
    }
    else {
        Write-Host "Khong co thay doi moi de commit. Se kiem tra/push commit hien tai." -ForegroundColor DarkYellow
    }

    Write-Step "Push origin/main"
    # Abort a truly stalled HTTP transfer instead of hanging forever. Overall process timeout remains the final guard.
    $pushArgs = @(
        "-c", "http.lowSpeedLimit=1024",
        "-c", "http.lowSpeedTime=90",
        "push", "-u", "origin", "HEAD:$RemoteBranch"
    )
    $pushResult = Invoke-GitWithTimeout -Arguments $pushArgs -TimeoutSeconds $PushTimeoutSeconds -Label "git push"
    Assert-GitSuccess $pushResult "git push"

    Write-Host "`n=== PUSH THANH CONG ===" -ForegroundColor Green
    Write-Host "Remote: origin/$RemoteBranch" -ForegroundColor Green
    Write-Host "Quick Push khong stage Library, Temp, Logs, .vs, UserSettings." -ForegroundColor Green
    exit 0
}
catch {
    Write-Host "`n=== QUICK PUSH THAT BAI ===" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "`nProcess se thoat, khong treo vo han." -ForegroundColor Yellow
    exit 1
}
finally {
    if ($hasMutex) {
        try { $mutex.ReleaseMutex() } catch {}
    }
    if ($null -ne $mutex) {
        $mutex.Dispose()
    }
}
