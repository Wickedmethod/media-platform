$h = @{ Authorization = "Bearer dev-token" }
$body = @{ url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"; title = "Rick Astley" } | ConvertTo-Json

try {
    $add = Invoke-RestMethod -Uri "http://localhost:3000/api/v1/queue/add" -Method POST -ContentType "application/json" -Body $body -Headers $h
    Write-Host "Added: $($add.title)"
} catch {
    Write-Host "Add error: $($_.Exception.Message)"
}

Start-Sleep 1

try {
    $play = Invoke-RestMethod -Uri "http://localhost:3000/api/v1/player/play" -Method POST -ContentType "application/json" -Headers $h
    Write-Host "State: $($play.state)"
    Write-Host "Track: $($play.currentItem.title)"
} catch {
    Write-Host "Play error: $($_.Exception.Message)"
}
