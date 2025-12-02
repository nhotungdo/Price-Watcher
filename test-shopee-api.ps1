# Test Shopee API directly
$keyword = "iPhone"
$url = "https://shopee.vn/api/v4/search/search_items?by=relevancy&order=desc&keyword=$([uri]::EscapeDataString($keyword))&limit=5&newest=0"

Write-Host "Testing Shopee API..." -ForegroundColor Cyan
Write-Host "URL: $url" -ForegroundColor Yellow

try {
    $headers = @{
        "User-Agent" = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
        "Referer" = "https://shopee.vn/search?keyword=$([uri]::EscapeDataString($keyword))"
        "x-api-source" = "pc"
        "x-shopee-language" = "vi"
        "x-requested-with" = "XMLHttpRequest"
        "Accept" = "application/json"
    }
    
    $response = Invoke-WebRequest -Uri $url -Headers $headers -Method Get
    
    Write-Host "`nStatus Code: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Content Length: $($response.Content.Length) bytes" -ForegroundColor Green
    
    $json = $response.Content | ConvertFrom-Json
    
    Write-Host "`nResponse Properties:" -ForegroundColor Cyan
    $json.PSObject.Properties.Name | ForEach-Object { Write-Host "  - $_" }
    
    if ($json.items) {
        Write-Host "`nItems Count: $($json.items.Count)" -ForegroundColor Green
        if ($json.items.Count -gt 0) {
            Write-Host "`nFirst Item:" -ForegroundColor Cyan
            $json.items[0] | ConvertTo-Json -Depth 3
        }
    } else {
        Write-Host "`nNo 'items' property found!" -ForegroundColor Red
        Write-Host "`nFull Response (first 500 chars):" -ForegroundColor Yellow
        $response.Content.Substring(0, [Math]::Min(500, $response.Content.Length))
    }
    
} catch {
    Write-Host "`nError: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}
