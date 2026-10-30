param([string]$BaseUrl = 'http://localhost:8080')

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
# Deliberately limited to MCP-only scenarios; none of these requests access the device.
$files = @(
    'smoke/01_mcp_initialize.http', 'smoke/02_mcp_tools_list.http',
    'smoke/03_mcp_prompts_list.http', 'smoke/04_mcp_prompts_get.http',
    'smoke/05_mcp_resources_list.http', 'smoke/06_mcp_resources_read.http',
    'modern/01_discovery.http', 'modern/02_basic_task.http'
)

function Read-RpcResult($response) {
    if (-not $response.Content) { return $null }
    $body = [string]$response.Content
    if ($body.TrimStart().StartsWith('{')) { $rpc = $body | ConvertFrom-Json -AsHashtable }
    else {
        $payload = $body -split "`n" | Where-Object { $_.StartsWith('data:') } |
            ForEach-Object { $_.Substring(5).Trim() | ConvertFrom-Json -AsHashtable } |
            Where-Object { $_.ContainsKey('id') } | Select-Object -Last 1
        $rpc = $payload
    }
    if (-not $rpc) { throw 'No JSON-RPC response found' }
    if ($rpc.error) { throw ($rpc.error | ConvertTo-Json -Compress) }
    return $rpc.result
}

foreach ($file in $files) {
    $variables = @{ host = $BaseUrl.TrimEnd('/'); taskId = '' }
    $sessionId = $null
    try {
        $content = [IO.File]::ReadAllText((Join-Path $root "examples_http/$file"))
        $requests = [regex]::Matches($content, '(?ms)^(POST|GET|DELETE) (.+?)\r?\n(.*?)(?=^###|\z)')
        foreach ($match in $requests) {
            $requestText = $match.Value
            foreach ($key in $variables.Keys) { $requestText = $requestText.Replace("{{$key}}", [string]$variables[$key]) }
            if ($sessionId) { $requestText = $requestText.Replace('{{init.response.headers.Mcp-Session-Id}}', $sessionId) }
            if ($requestText.Contains('{{')) { throw "Unresolved variable in $file" }
            $lines = $requestText -split '\r?\n'
            $method, $uri = $lines[0] -split ' ', 2
            $headers = @{}
            $index = 1
            while ($index -lt $lines.Length -and $lines[$index].Trim()) {
                $name, $value = $lines[$index] -split ':\s*', 2
                $headers[$name] = $value
                $index++
            }
            $body = ($lines[($index + 1)..($lines.Length - 1)] -join "`n").Trim()
            $response = Invoke-WebRequest $uri -Method $method -Headers $headers -Body $body -TimeoutSec 15
            if ($response.Headers.ContainsKey('Mcp-Session-Id')) { $sessionId = [string]$response.Headers['Mcp-Session-Id'][0] }
            if ($response.StatusCode -eq 202) { continue }
            $result = Read-RpcResult $response
            if ($result.taskId) { $variables.taskId = $result.taskId }
            $rpcBody = $body | ConvertFrom-Json -AsHashtable
            if ($rpcBody.method -eq 'tasks/get') {
                $deadline = [DateTimeOffset]::UtcNow.AddSeconds(10)
                while ($result.status -eq 'working') {
                    if ([DateTimeOffset]::UtcNow -ge $deadline) { throw 'Task polling timed out' }
                    Start-Sleep -Milliseconds 25
                    $result = Read-RpcResult (Invoke-WebRequest $uri -Method $method -Headers $headers -Body $body -TimeoutSec 15)
                }
                if ($result.status -ne 'completed' -or -not $result.result.structuredContent.prgDataBase64) {
                    throw "PRG task did not complete successfully: $($result | ConvertTo-Json -Depth 20)"
                }
            }
        }
        Write-Output "PASS $file"
    }
    finally {
        if ($sessionId) {
            $null = Invoke-WebRequest "$($BaseUrl.TrimEnd('/'))/" -Method Delete -Headers @{
                'Mcp-Session-Id' = $sessionId; 'MCP-Protocol-Version' = '2025-11-25'
            } -TimeoutSec 10
        }
    }
}
