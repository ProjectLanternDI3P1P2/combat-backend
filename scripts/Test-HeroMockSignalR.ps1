param(
    [string]$BaseUrl = "http://127.0.0.1:5099",
    [string]$GrpcUrl = "http://127.0.0.1:5100",
    [switch]$StartServer
)

$ErrorActionPreference = "Stop"
$recordSeparator = [char]0x1e
$serverProcess = $null

function Assert-Condition {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Send-HubMessage {
    param(
        [System.Net.WebSockets.ClientWebSocket]$Socket,
        [string]$Json
    )

    $payload = [System.Text.Encoding]::UTF8.GetBytes($Json + $recordSeparator)
    $segment = [ArraySegment[byte]]::new($payload)
    [void]$Socket.SendAsync(
        $segment,
        [System.Net.WebSockets.WebSocketMessageType]::Text,
        $true,
        [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()
}

function Receive-HubMessage {
    param(
        [System.Net.WebSockets.ClientWebSocket]$Socket,
        [System.Collections.Generic.Queue[object]]$PendingMessages
    )

    while ($PendingMessages.Count -eq 0) {
        $buffer = [byte[]]::new(8192)
        $stream = [System.IO.MemoryStream]::new()
        $timeout = [System.Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(10))

        try {
            do {
                $segment = [ArraySegment[byte]]::new($buffer)
                $result = $Socket.ReceiveAsync($segment, $timeout.Token).GetAwaiter().GetResult()

                if ($result.MessageType -eq [System.Net.WebSockets.WebSocketMessageType]::Close) {
                    throw "The SignalR WebSocket closed before the expected message arrived."
                }

                $stream.Write($buffer, 0, $result.Count)
            } while (-not $result.EndOfMessage)

            $text = [System.Text.Encoding]::UTF8.GetString($stream.ToArray())
            foreach ($record in $text.Split($recordSeparator, [StringSplitOptions]::RemoveEmptyEntries)) {
                $PendingMessages.Enqueue(($record | ConvertFrom-Json))
            }
        }
        finally {
            $timeout.Dispose()
            $stream.Dispose()
        }
    }

    return $PendingMessages.Dequeue()
}

function Receive-StateChanged {
    param(
        [System.Net.WebSockets.ClientWebSocket]$Socket,
        [System.Collections.Generic.Queue[object]]$PendingMessages
    )

    while ($true) {
        $message = Receive-HubMessage -Socket $Socket -PendingMessages $PendingMessages
        if ($message.type -eq 1 -and $message.target -eq "CombatStateChanged") {
            return $message.arguments[0]
        }
    }
}

function Invoke-HubMethod {
    param(
        [System.Net.WebSockets.ClientWebSocket]$Socket,
        [System.Collections.Generic.Queue[object]]$PendingMessages,
        [string]$InvocationId,
        [string]$Target
    )

    $invocation = @{
        type = 1
        invocationId = $InvocationId
        target = $Target
        arguments = @()
    } | ConvertTo-Json -Compress

    Send-HubMessage -Socket $Socket -Json $invocation

    $notification = $null
    $completion = $null
    while ($null -eq $notification -or $null -eq $completion) {
        $message = Receive-HubMessage -Socket $Socket -PendingMessages $PendingMessages

        if ($message.type -eq 1 -and $message.target -eq "CombatStateChanged") {
            $notification = $message.arguments[0]
        }
        elseif ($message.type -eq 3 -and $message.invocationId -eq $InvocationId) {
            if ($null -ne $message.error) {
                throw "SignalR invocation '$Target' failed: $($message.error)"
            }

            $completion = $message
        }
    }

    return @{
        Notification = $notification
        Result = $completion.result
    }
}

function Connect-HeroScenario {
    param([string]$Scenario)

    $hubUrl = "$BaseUrl/hubs/combats"
    $negotiateUrl = "$hubUrl/negotiate?negotiateVersion=1&scenario=$([Uri]::EscapeDataString($Scenario))"
    $negotiation = Invoke-RestMethod -Method Post -Uri $negotiateUrl
    Assert-Condition ($null -ne $negotiation.connectionToken) "SignalR negotiation did not return a connection token."

    $webSocketUrl = $hubUrl.Replace("http://", "ws://").Replace("https://", "wss://")
    $webSocketUrl += "?id=$([Uri]::EscapeDataString($negotiation.connectionToken))&scenario=$([Uri]::EscapeDataString($Scenario))"

    $socket = [System.Net.WebSockets.ClientWebSocket]::new()
    [void]$socket.ConnectAsync(
        [Uri]$webSocketUrl,
        [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()
    Send-HubMessage -Socket $socket -Json '{"protocol":"json","version":1}'

    $messages = [System.Collections.Generic.Queue[object]]::new()
    $handshake = Receive-HubMessage -Socket $socket -PendingMessages $messages
    Assert-Condition ($null -eq $handshake.error) "SignalR handshake failed: $($handshake.error)"

    $loading = Receive-StateChanged -Socket $socket -PendingMessages $messages
    $initial = Receive-StateChanged -Socket $socket -PendingMessages $messages

    Assert-Condition ($loading.availability -eq "loading") "Scenario '$Scenario' did not publish loading first."
    Assert-Condition ($null -eq $loading.hero) "Loading state for '$Scenario' unexpectedly contained a hero."
    Assert-Condition ($initial.availability -eq "ready") "Scenario '$Scenario' did not publish a ready snapshot."
    Assert-Condition ($initial.scenario -eq $Scenario) "Scenario '$Scenario' returned the wrong scenario name."

    return @{
        Socket = $socket
        Messages = $messages
        Initial = $initial
    }
}

function Close-HeroScenario {
    param([System.Net.WebSockets.ClientWebSocket]$Socket)

    $Socket.Dispose()
}

try {
    if ($StartServer) {
        $repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
        $standardOutput = Join-Path ([System.IO.Path]::GetTempPath()) "combat-hero-mock.stdout.log"
        $standardError = Join-Path ([System.IO.Path]::GetTempPath()) "combat-hero-mock.stderr.log"

        $previousEnvironment = @{
            ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT
            HeroMock__Enabled = $env:HeroMock__Enabled
            Kestrel__Endpoints__Http__Url = $env:Kestrel__Endpoints__Http__Url
            Kestrel__Endpoints__Grpc__Url = $env:Kestrel__Endpoints__Grpc__Url
        }

        try {
            $env:ASPNETCORE_ENVIRONMENT = "Development"
            $env:HeroMock__Enabled = "true"
            $env:Kestrel__Endpoints__Http__Url = $BaseUrl
            $env:Kestrel__Endpoints__Grpc__Url = $GrpcUrl

            $serverProcess = Start-Process dotnet `
                -ArgumentList @("run", "--no-build", "--no-launch-profile", "--project", "Combat.Presentation/Combat.Presentation.csproj") `
                -WorkingDirectory $repositoryRoot `
                -RedirectStandardOutput $standardOutput `
                -RedirectStandardError $standardError `
                -WindowStyle Hidden `
                -PassThru
        }
        finally {
            $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment.ASPNETCORE_ENVIRONMENT
            $env:HeroMock__Enabled = $previousEnvironment.HeroMock__Enabled
            $env:Kestrel__Endpoints__Http__Url = $previousEnvironment.Kestrel__Endpoints__Http__Url
            $env:Kestrel__Endpoints__Grpc__Url = $previousEnvironment.Kestrel__Endpoints__Grpc__Url
        }

        $ready = $false
        for ($attempt = 0; $attempt -lt 30; $attempt++) {
            if ($serverProcess.HasExited) {
                $details = Get-Content -Raw $standardError -ErrorAction SilentlyContinue
                throw "The backend exited before becoming ready. $details"
            }

            try {
                $response = Invoke-WebRequest -Uri "$BaseUrl/health/live" -TimeoutSec 1 -UseBasicParsing
                if ($response.StatusCode -eq 200) {
                    $ready = $true
                    break
                }
            }
            catch {
                Start-Sleep -Milliseconds 250
            }
        }

        Assert-Condition $ready "The backend did not become ready at $BaseUrl."
    }

    $complete = Connect-HeroScenario -Scenario "complete"
    try {
        Assert-Condition ($complete.Initial.hero.currentHealth -eq 84) "Complete scenario health is incorrect."
        Assert-Condition ($complete.Initial.hero.abilities.Count -eq 2) "Complete scenario abilities are missing."
        Assert-Condition ($complete.Initial.hero.equipment.Count -eq 1) "Complete scenario equipment is missing."
        Assert-Condition ($complete.Initial.hero.consumables.Count -eq 1) "Complete scenario consumables are missing."
    }
    finally {
        Close-HeroScenario -Socket $complete.Socket
    }

    $zero = Connect-HeroScenario -Scenario "zero"
    try {
        Assert-Condition ($zero.Initial.hero.currentHealth -eq 0) "Zero health was not preserved."
        Assert-Condition ($zero.Initial.hero.currentMana -eq 0) "Zero mana was not preserved."
        Assert-Condition ($zero.Initial.hero.consumables[0].quantity -eq 0) "Zero quantity was not preserved."
    }
    finally {
        Close-HeroScenario -Socket $zero.Socket
    }

    $empty = Connect-HeroScenario -Scenario "empty"
    try {
        Assert-Condition ($empty.Initial.hero.abilities.Count -eq 0) "Empty abilities were not preserved."
        Assert-Condition ($empty.Initial.hero.equipment.Count -eq 0) "Empty equipment was not preserved."
        Assert-Condition ($empty.Initial.hero.consumables.Count -eq 0) "Empty consumables were not preserved."
    }
    finally {
        Close-HeroScenario -Socket $empty.Socket
    }

    $partial = Connect-HeroScenario -Scenario "partial"
    try {
        Assert-Condition ($partial.Initial.hero.currentHealth -eq 38) "Partial scenario present health is incorrect."
        Assert-Condition ($null -eq $partial.Initial.hero.maxHealth) "Partial scenario missing max health was not preserved."
        Assert-Condition ($null -eq $partial.Initial.hero.equipment) "Partial scenario missing equipment was not preserved."
    }
    finally {
        Close-HeroScenario -Socket $partial.Socket
    }

    $changing = Connect-HeroScenario -Scenario "changing"
    try {
        $resynchronized = Invoke-HubMethod `
            -Socket $changing.Socket `
            -PendingMessages $changing.Messages `
            -InvocationId "resynchronize-1" `
            -Target "Resynchronize"
        Assert-Condition ($resynchronized.Result.sequence -eq 1) "Resynchronization returned the wrong sequence."
        Assert-Condition ($resynchronized.Notification.sequence -eq 1) "Resynchronization did not republish the current state."

        $advanced = Invoke-HubMethod `
            -Socket $changing.Socket `
            -PendingMessages $changing.Messages `
            -InvocationId "advance-1" `
            -Target "AdvanceMockState"
        Assert-Condition ($advanced.Result.sequence -eq 2) "Mock update returned the wrong sequence."
        Assert-Condition ($advanced.Notification.sequence -eq 2) "Mock update notification was not published."
        Assert-Condition ($advanced.Notification.hero.currentHealth -eq 57) "Mock update health was not transported."
        Assert-Condition ($advanced.Notification.hero.currentMana -eq 18) "Mock update mana was not transported."
    }
    finally {
        Close-HeroScenario -Socket $changing.Socket
    }

    if ($StartServer) {
        if ($null -ne $serverProcess -and -not $serverProcess.HasExited) {
            Stop-Process -Id $serverProcess.Id
            $serverProcess.WaitForExit()
        }

        $serverProcess = $null
        $productionOutput = Join-Path ([System.IO.Path]::GetTempPath()) "combat-hero-disabled.stdout.log"
        $productionError = Join-Path ([System.IO.Path]::GetTempPath()) "combat-hero-disabled.stderr.log"
        $previousEnvironment = @{
            ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT
            HeroMock__Enabled = $env:HeroMock__Enabled
            Kestrel__Endpoints__Http__Url = $env:Kestrel__Endpoints__Http__Url
            Kestrel__Endpoints__Grpc__Url = $env:Kestrel__Endpoints__Grpc__Url
        }

        try {
            $env:ASPNETCORE_ENVIRONMENT = "Production"
            $env:HeroMock__Enabled = "true"
            $env:Kestrel__Endpoints__Http__Url = $BaseUrl
            $env:Kestrel__Endpoints__Grpc__Url = $GrpcUrl

            $serverProcess = Start-Process dotnet `
                -ArgumentList @("run", "--no-build", "--no-launch-profile", "--project", "Combat.Presentation/Combat.Presentation.csproj") `
                -WorkingDirectory $repositoryRoot `
                -RedirectStandardOutput $productionOutput `
                -RedirectStandardError $productionError `
                -WindowStyle Hidden `
                -PassThru
        }
        finally {
            $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment.ASPNETCORE_ENVIRONMENT
            $env:HeroMock__Enabled = $previousEnvironment.HeroMock__Enabled
            $env:Kestrel__Endpoints__Http__Url = $previousEnvironment.Kestrel__Endpoints__Http__Url
            $env:Kestrel__Endpoints__Grpc__Url = $previousEnvironment.Kestrel__Endpoints__Grpc__Url
        }

        $ready = $false
        for ($attempt = 0; $attempt -lt 30; $attempt++) {
            if ($serverProcess.HasExited) {
                $details = Get-Content -Raw $productionError -ErrorAction SilentlyContinue
                throw "The backend exited with the Hero mock disabled. $details"
            }

            try {
                $response = Invoke-WebRequest -Uri "$BaseUrl/health/live" -TimeoutSec 1 -UseBasicParsing
                if ($response.StatusCode -eq 200) {
                    $ready = $true
                    break
                }
            }
            catch {
                Start-Sleep -Milliseconds 250
            }
        }

        Assert-Condition $ready "The backend did not start with the Hero mock disabled."

        $hubStatusCode = $null
        try {
            Invoke-WebRequest `
                -Method Post `
                -Uri "$BaseUrl/hubs/combats/negotiate?negotiateVersion=1" `
                -TimeoutSec 2 `
                -UseBasicParsing | Out-Null
            $hubStatusCode = 200
        }
        catch {
            if ($null -eq $_.Exception.Response) {
                throw
            }

            $hubStatusCode = [int]$_.Exception.Response.StatusCode
        }

        Assert-Condition `
            ($hubStatusCode -eq 404) `
            "The Hero mock hub was exposed outside Development (HTTP $hubStatusCode)."
    }

    Write-Host "SignalR hero mock smoke test passed for all scenarios and production gating."
}
finally {
    if ($null -ne $serverProcess -and -not $serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id
        $serverProcess.WaitForExit()
    }
}
