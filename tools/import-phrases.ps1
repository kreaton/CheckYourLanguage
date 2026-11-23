# Imports the phrase list and its recordings from the lydfiler folder into the app:
#   - phrases-all.csv (one column per language code, one phrase per row, the rows aligned) becomes
#     CheckYourLanguage/wwwroot/phrases.json, with the text and audio file name of every phrase;
#   - audio/<code>/<file>.mp3 is copied to CheckYourLanguage/wwwroot/audio/<code>/.
# A file name is the phrase in lower case without accents (ae/oe/aa for the Danish letters), with
# every run of characters other than letters, digits, _ and - turned into one _ ("j'ai une
# question" -> j_ai_une_question). A name Windows reserves for a device (con, nul, com1, ...) gets
# a trailing _ in the app (con_.mp3), as git and the build cannot handle such files on Windows; .NET
# file calls are used throughout, as PowerShell's own cannot see them. The script stops if any
# recording is missing.
#   powershell -File tools\import-phrases.ps1 [-Source ..\..\lydfiler]
param(
    [string]$Source = (Join-Path $PSScriptRoot "..\..\..\lydfiler")
)

$ErrorActionPreference = "Stop"
$Source = (Resolve-Path $Source).Path
$app = Join-Path $PSScriptRoot "..\CheckYourLanguage\wwwroot"

function Get-Slug([string]$text) {
    # The Danish letters are written with \u escapes, as Windows PowerShell reads a script without
    # a BOM as ANSI.
    $s = $text.Trim().ToLowerInvariant()
    $s = $s.Replace([string][char]0x00E6, "ae").Replace([string][char]0x00F8, "oe").Replace([string][char]0x00E5, "aa").Replace([string][char]0x00DF, "ss")
    $s = $s.Normalize([System.Text.NormalizationForm]::FormKD)
    $s = -join ($s.ToCharArray() | Where-Object {
        [System.Globalization.CharUnicodeInfo]::GetUnicodeCategory($_) -ne [System.Globalization.UnicodeCategory]::NonSpacingMark
    })
    $s = [regex]::Replace($s, "[^A-Za-z0-9_\-]+", "_").Trim("_")
    if ($s) { $s } else { "word" }
}

# The name the recording gets in the app.
function Get-AppName([string]$slug) {
    if ($slug -match "^(con|prn|aux|nul|com[0-9]|lpt[0-9])$") { "${slug}_" } else { $slug }
}

$rows = @(Import-Csv -Path (Join-Path $Source "phrases-all.csv") -Encoding UTF8)
$codes = @($rows[0].PSObject.Properties.Name | ForEach-Object { $_.Trim([char]0xFEFF) })
Write-Host "$($rows.Count) phrases in $($codes -join ', ')"

$missing = @()
$phrases = foreach ($row in $rows) {
    $props = @($row.PSObject.Properties)
    $text = @(); $file = @()
    for ($i = 0; $i -lt $codes.Count; $i++) {
        $t = ([string]$props[$i].Value).Trim()
        $f = Get-Slug $t
        if (-not [System.IO.File]::Exists((Join-Path $Source "audio\$($codes[$i])\$f.mp3"))) {
            $missing += "$($codes[$i])/$f.mp3 ($t)"
        }
        $text += $t; $file += $f
    }
    [ordered]@{ text = $text; file = $file }
}

if ($missing) {
    $missing | ForEach-Object { Write-Warning "Missing: $_" }
    throw "$($missing.Count) recordings are missing; nothing was imported."
}

# Copies only the recordings the phrases use, and removes any that are no longer used.
foreach ($i in 0..($codes.Count - 1)) {
    $code = $codes[$i]
    $dir = Join-Path $app "audio\$code"
    New-Item -ItemType Directory -Force $dir | Out-Null
    $used = @($phrases | ForEach-Object { $_.file[$i] } | Sort-Object -Unique)
    foreach ($f in $used) {
        [System.IO.File]::Copy((Join-Path $Source "audio\$code\$f.mp3"), (Join-Path $dir "$(Get-AppName $f).mp3"), $true)
    }
    $keep = @{}; $used | ForEach-Object { $keep["$(Get-AppName $_).mp3"] = $true }
    Get-ChildItem -LiteralPath $dir -File | Where-Object { -not $keep[$_.Name] } | Remove-Item
    Write-Host ("{0}: {1} recordings" -f $code, $used.Count)
}

foreach ($p in $phrases) { $p.file = @($p.file | ForEach-Object { Get-AppName $_ }) }
$json = [ordered]@{ languages = $codes; phrases = @($phrases) } | ConvertTo-Json -Depth 5 -Compress
[System.IO.File]::WriteAllText((Join-Path $app "phrases.json"), $json, (New-Object System.Text.UTF8Encoding $false))
Write-Host "Wrote $(Join-Path $app 'phrases.json')"
