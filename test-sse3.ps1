# Test SSE directly against API (bypass nginx)
$url = "http://localhost:5300/api/events"
Write-Host "Testing SSE directly at $url..."

try {
    $wc = New-Object System.Net.WebClient
    $wc.Headers.Add("Authorization", "Bearer dev-token")
    $wc.Headers.Add("Accept", "text/event-stream")
    
    $stream = $wc.OpenRead($url)
    $reader = New-Object System.IO.StreamReader($stream)
    
    Write-Host "Connected! Reading..."
    
    $deadline = (Get-Date).AddSeconds(5)
    $lineCount = 0
    while ((Get-Date) -lt $deadline -and -not $reader.EndOfStream) {
        $line = $reader.ReadLine()
        $lineCount++
        Write-Host "LINE $lineCount : [$line]"
        if ($lineCount -ge 20) { break }
    }
    
    Write-Host "Read $lineCount lines total."
    $reader.Close()
    $stream.Close()
} catch {
    Write-Host "Error: $($_.Exception.Message)"
}
