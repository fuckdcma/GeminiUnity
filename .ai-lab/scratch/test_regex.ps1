$content = Get-Content 'Output\Studio\Assets\Fonts\Font_Mapping_Config.json' -Raw
$blockMatches = [regex]::Matches($content, '\{([^{}]+)\}', [System.Text.RegularExpressions.RegexOptions]::Singleline)
Write-Output "Total blocks matched: $($blockMatches.Count)"
foreach ($b in $blockMatches) {
    $text = $b.Groups[1].Value
    $mOrig = [regex]::Match($text, '"OriginalFont"\s*:\s*"([^"]+)"')
    $mTarget = [regex]::Match($text, '"TargetReplacement"\s*:\s*"([^"]+)"')
    $isProtected = $text.Contains('"IsProtected": true')
    if ($mOrig.Success -and -not $isProtected) {
        $orig = $mOrig.Groups[1].Value
        $target = $mTarget.Groups[1].Value
        if ($target -ne 'KEEP_OR_FALLBACK' -and $target -ne 'KEEP_ORIGINAL' -and $target -ne '') {
            Write-Output "Mapped: '$orig' -> '$target'"
        }
    }
}
