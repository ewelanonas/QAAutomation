#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Scans files for credential-shaped values before they can be committed.

.DESCRIPTION
    Written because .agents/ is committed on purpose. Those review reports and plans
    quote source code and configuration, so a real key can be carried into the
    repository by accident. This script is the mechanical check that stops it.

    By default it scans the STAGED content, not the working copy, because staged
    content is what a commit will actually publish. It reads each staged blob with
    "git show :<path>" so a partially staged file is judged on the staged half.

    Findings are reported as file:line with the matched value MASKED. The scanner
    never prints a secret: doing so would copy it into the terminal scrollback and,
    in CI, into the build log - which is the problem it exists to prevent.

.PARAMETER Scope
    Staged  - staged content only. The default, and what the pre-commit hook uses.
    Tracked - every tracked text file. Use for an audit of what is already committed.
    Path    - the files or directories given in -Path.

.PARAMETER Path
    Files or directories to scan when -Scope Path is used.

.PARAMETER Quiet
    Print nothing on a clean result. Findings are always printed.

.OUTPUTS
    Exit 0 - no findings.
    Exit 1 - at least one finding. The pre-commit hook treats this as a block.
    Exit 2 - the scanner itself could not run. Also treated as a block, because a
             security check that errors must fail closed, never open.

.EXAMPLE
    ./scripts/Find-StagedSecrets.ps1
    Scans what is currently staged. This is what the pre-commit hook runs.

.EXAMPLE
    ./scripts/Find-StagedSecrets.ps1 -Scope Tracked
    Audits every tracked file in the repository.

.EXAMPLE
    ./scripts/Find-StagedSecrets.ps1 -Scope Path -Path .agents
    Scans the .agents folder in the working copy.
#>
[CmdletBinding()]
param(
    [ValidateSet('Staged', 'Tracked', 'Path')]
    [string] $Scope = 'Staged',

    [string[]] $Path = @(),

    [switch] $Quiet
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# What counts as a secret.
#
# Each rule is deliberately narrow. A broad rule that fires on the word
# "password" would flag every steering file that says "never commit a password",
# the scanner would be switched off within a week, and we would be worse off
# than having no scanner at all. So every rule here matches a VALUE, not prose.
# ---------------------------------------------------------------------------
$rules = @(
    @{ Name = 'GitHub token';              Pattern = 'gh[pousr]_[A-Za-z0-9]{30,}' }
    @{ Name = 'GitHub fine-grained PAT';   Pattern = 'github_pat_[A-Za-z0-9_]{30,}' }
    @{ Name = 'AWS access key id';         Pattern = '\b(?:AKIA|ASIA)[0-9A-Z]{16}\b' }
    @{ Name = 'AWS secret access key';     Pattern = '(?i)aws_?secret_?access_?key\s*[:=]\s*["'']?[A-Za-z0-9/+=]{40}' }
    @{ Name = 'Azure storage key';         Pattern = '(?i)AccountKey\s*=\s*[A-Za-z0-9/+=]{40,}' }
    @{ Name = 'Google API key';            Pattern = '\bAIza[0-9A-Za-z_-]{35}\b' }
    @{ Name = 'Slack token';               Pattern = '\bxox[baprs]-[0-9A-Za-z-]{10,}' }
    @{ Name = 'NuGet API key';             Pattern = '\boy2[a-z0-9]{43}\b' }
    @{ Name = 'Private key block';         Pattern = '-----BEGIN (?:RSA |EC |OPENSSH |PGP |DSA )?PRIVATE KEY-----' }
    @{ Name = 'JSON web token';            Pattern = '\beyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}' }
    @{ Name = 'Bearer token value';        Pattern = '(?i)\bbearer\s+[A-Za-z0-9\-._~+/]{20,}={0,2}' }
    @{ Name = 'Basic auth value';          Pattern = '(?i)\bbasic\s+[A-Za-z0-9+/]{16,}={0,2}' }
    @{ Name = 'URL with inline password';  Pattern = '[a-z][a-z0-9+.-]*://[^/\s:@]+:[^/\s:@]+@' }
    @{ Name = 'SQL password in conn str';  Pattern = '(?i)(?:password|pwd)\s*=\s*[^;\s"''<]{4,}\s*;' }
    @{ Name = 'Secret-shaped assignment';  Pattern = '(?i)\b(?:password|passwd|api[_-]?key|apikey|client[_-]?secret|access[_-]?token|auth[_-]?token|secret[_-]?key|private[_-]?key|connection[_-]?string)\b\s*[:=]\s*["''][^"'']{6,}["'']' }
)

# Values that look like secrets but are not. A placeholder is the CORRECT thing to
# commit, so matching one is a pass, not a finding.
$placeholderPattern = '(?i)YOUR_|_HERE|<[a-z_ -]+>|\bexample\b|\bplaceholder\b|\bdummy\b|\bsample\b|\bredacted\b|\bchangeme\b|\bchange-me\b|\bTODO\b|\bFIXME\b|\bfake\b|\bxxxx|\*\*\*|\.\.\.|\bnotarealkey\b|\$\(|\$\{|%[A-Z_]+%|\{\{'

# Paths never worth scanning: build output, binaries, lock files and this scanner
# itself (it contains every pattern above, so it would flag its own source).
$excludedPathPattern = '(?i)(^|[\\/])(bin|obj|node_modules|\.vs|\.git|test-results|TestResults|allure-results)[\\/]|\.(dll|exe|pdb|png|jpg|jpeg|gif|ico|pdf|zip|nupkg|snk|trx)$|[\\/]Find-StagedSecrets\.ps1$|[\\/]pre-commit$'

# ---------------------------------------------------------------------------

function Write-Info {
    param([string] $Message)

    if (-not $Quiet) {
        Write-Host $Message
    }
}

function Get-MaskedExcerpt {
    param(
        [string] $Line,
        [string] $Match
    )

    # Show enough to locate the value, never enough to use it.
    if ($Match.Length -le 8) {
        $masked = '*' * $Match.Length
    }
    else {
        $masked = $Match.Substring(0, 4) + ('*' * 8) + $Match.Substring($Match.Length - 2, 2)
    }

    $excerpt = $Line.Replace($Match, $masked).Trim()
    if ($excerpt.Length -gt 140) {
        $excerpt = $excerpt.Substring(0, 140) + ' ...'
    }

    return $excerpt
}

function Get-FileList {
    if ($Scope -eq 'Staged') {
        $output = & git diff --cached --name-only --diff-filter=ACMR
        if ($LASTEXITCODE -ne 0) {
            throw 'git diff --cached failed. Is this a git repository?'
        }
    }
    elseif ($Scope -eq 'Tracked') {
        $output = & git ls-files
        if ($LASTEXITCODE -ne 0) {
            throw 'git ls-files failed. Is this a git repository?'
        }
    }
    else {
        if ($Path.Count -eq 0) {
            throw '-Scope Path requires -Path.'
        }

        $collected = New-Object System.Collections.Generic.List[string]
        foreach ($entry in $Path) {
            if (Test-Path -LiteralPath $entry -PathType Container) {
                $children = Get-ChildItem -LiteralPath $entry -Recurse -File
                foreach ($child in $children) {
                    $collected.Add($child.FullName)
                }
            }
            elseif (Test-Path -LiteralPath $entry) {
                $collected.Add((Resolve-Path -LiteralPath $entry).Path)
            }
            else {
                throw "Path not found: $entry"
            }
        }

        $output = $collected.ToArray()
    }

    $kept = New-Object System.Collections.Generic.List[string]
    foreach ($file in $output) {
        if ([string]::IsNullOrWhiteSpace($file)) {
            continue
        }

        if ($file -match $excludedPathPattern) {
            continue
        }

        $kept.Add($file)
    }

    # Returned plain. The caller wraps this in @(), which gives a real array for
    # zero, one or many results - so .Count is always safe under Set-StrictMode.
    # Do not add a leading comma here as well: that double-wraps the array and the
    # caller ends up iterating one element that is the whole array.
    return $kept.ToArray()
}

function Get-FileContent {
    param([string] $File)

    if ($Scope -eq 'Staged') {
        # Read the staged blob, not the working copy. A file can be staged clean and
        # then dirtied, or staged dirty and then cleaned; only the staged half ships.
        $content = & git show ":$File" 2>$null
        if ($LASTEXITCODE -ne 0) {
            return $null
        }

        return $content
    }

    if (-not (Test-Path -LiteralPath $File)) {
        return $null
    }

    return Get-Content -LiteralPath $File -ErrorAction SilentlyContinue
}

# ---------------------------------------------------------------------------

$findings = New-Object System.Collections.Generic.List[object]
$scannedCount = 0

try {
    # @() guarantees an array even for zero or one result, so .Count is always safe.
    $files = @(Get-FileList)

    if ($files.Count -eq 0) {
        Write-Info 'Secret scan: nothing to scan.'
        exit 0
    }

    foreach ($file in $files) {
        $lines = @(Get-FileContent -File $file)
        if ($lines.Count -eq 0) {
            continue
        }

        $scannedCount++
        $lineNumber = 0

        foreach ($line in $lines) {
            $lineNumber++

            if ([string]::IsNullOrWhiteSpace($line)) {
                continue
            }

            foreach ($rule in $rules) {
                $matched = [regex]::Match($line, $rule.Pattern)
                if (-not $matched.Success) {
                    continue
                }

                # Test the MATCHED VALUE, not the whole line. Testing the line is
                # too generous: a real token on a line that happens to mention
                # "example" elsewhere would be waved through. A placeholder is only
                # a placeholder when the matched value itself is one.
                if ($matched.Value -match $placeholderPattern) {
                    continue
                }

                $findings.Add([pscustomobject]@{
                    File    = $file
                    Line    = $lineNumber
                    Rule    = $rule.Name
                    Excerpt = Get-MaskedExcerpt -Line $line -Match $matched.Value
                })
            }
        }
    }
}
catch {
    Write-Host "SCANNER ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'Failing closed: a security check that cannot run must block, not pass.' -ForegroundColor Red
    exit 2
}

if ($findings.Count -eq 0) {
    Write-Info "Secret scan: clean. $scannedCount file(s) scanned, scope $Scope."
    exit 0
}

Write-Host ''
Write-Host '================================================================' -ForegroundColor Red
Write-Host " BLOCKED: $($findings.Count) credential-shaped value(s) found" -ForegroundColor Red
Write-Host '================================================================' -ForegroundColor Red
Write-Host ''

foreach ($finding in $findings) {
    Write-Host ("  {0}:{1}" -f $finding.File, $finding.Line) -ForegroundColor Yellow
    Write-Host ("    rule    : {0}" -f $finding.Rule)
    Write-Host ("    content : {0}" -f $finding.Excerpt)
    Write-Host ''
}

Write-Host 'The matched values are masked above on purpose - printing them would copy'
Write-Host 'them into your terminal history and, in CI, into the build log.'
Write-Host ''
Write-Host 'What to do:'
Write-Host '  1. Replace the value with a placeholder such as YOUR_API_KEY_HERE.'
Write-Host '  2. Move the real value to an environment variable or the CI secret store.'
Write-Host '  3. If the value was ever pushed, treat it as leaked and ROTATE it. Removing'
Write-Host '     it from a later commit does not remove it from the history.'
Write-Host ''
Write-Host 'If this is a false positive, add a placeholder marker to the line or widen'
Write-Host 'the allow-list in scripts/Find-StagedSecrets.ps1. Do not reach for'
Write-Host '"git commit --no-verify" as the first move - that skips every check.'
Write-Host ''

exit 1
