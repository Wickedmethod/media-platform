# Test SSE through nginx proxy
$url = "http://localhost:3000/api/events"
Write-Host "Connecting to SSE at $url..."

$request = [System.Net.HttpWebRequest]::Create($url)
$request.Headers.Add("Authorization", "Bearer dev-token")
$request.Accept = "text/event-stream"
$request.AllowAutoRedirect = $true
$request.Timeout = 15000

try {
    $response = $request.GetResponse()
    $stream = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    
    Write-Host "Connected! Status: $($response.StatusCode)"
    Write-Host "Content-Type: $($response.ContentType)"
    Write-Host "Waiting for events (10 seconds)..."
    
    $deadline = (Get-Date).AddSeconds(10)
    while ((Get-Date) -lt $deadline) {
        if ($reader.Peek() -ge 0) {
            $line = $reader.ReadLine()
            Write-Host "SSE> $line"
        } else {
            Start-Sleep -Milliseconds 100
        }
    }
    
    $reader.Close()
    $response.Close()
    Write-Host "Done."
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    if ($_.Exception.InnerException) {
        Write-Host "Inner: $($_.Exception.InnerException.Message)"
    }
}
