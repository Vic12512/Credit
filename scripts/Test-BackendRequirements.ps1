param(
    [string]$BaseUrl = 'http://localhost:5041',
    [string]$AdminUsername = 'admin',
    [string]$AdminPassword = 'Admin_Local_2026!',
    [string]$ConsumerUsername = 'consumer.demo',
    [string]$ConsumerPassword = 'Consumer_Local_2026!',
    [string]$EntryUsername = 'dataentry.demo',
    [string]$EntryPassword = 'DataEntry_Local_2026!',
    [string]$ApiEnvPath = (Join-Path $PSScriptRoot '../src/Snic.Api/.env')
)

# Usa una base de demostración: conserva personas, productos y auditorías generados.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$client = New-Object System.Net.Http.HttpClient
$client.BaseAddress = [Uri]($BaseUrl.TrimEnd('/') + '/')
$script:Checks = 0
function Call([string]$Method, [string]$Path, [string]$Token = '', $Body = $null) {
    $req = New-Object System.Net.Http.HttpRequestMessage
    $req.Method = New-Object System.Net.Http.HttpMethod($Method)
    $req.RequestUri = [Uri]::new($Path.TrimStart('/'), [UriKind]::Relative)
    if ($Token) { $req.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Bearer', $Token) }
    if ($null -ne $Body) { $req.Content = New-Object System.Net.Http.StringContent((ConvertTo-Json -InputObject $Body -Depth 10 -Compress), [Text.Encoding]::UTF8, 'application/json') }
    try {
        $res = $client.SendAsync($req).GetAwaiter().GetResult()
        try {
            $raw = $res.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            $data = if ($raw) { ConvertFrom-Json -InputObject $raw } else { $null }
            return [pscustomobject]@{ Status = [int]$res.StatusCode; Data = $data }
        } finally { $res.Dispose() }
    } finally { $req.Dispose() }
}
function Check([bool]$Condition, [string]$Label) {
    if (-not $Condition) { throw "FALLO: $Label" }
    $script:Checks++
    Write-Output "OK - $Label"
}
function Login([string]$Username, [string]$Password) {
    $response = Call 'POST' 'api/v1/auth/login' '' @{username=$Username;password=$Password}
    if ($response.Status -ne 200) { throw "No fue posible iniciar sesion con $Username" }
    return $response.Data
}
try {
    $started = [DateTimeOffset]::UtcNow.AddSeconds(-1).ToString('o')
    $admin = Login $AdminUsername $AdminPassword
    $consumer = Login $ConsumerUsername $ConsumerPassword
    $entry = Login $EntryUsername $EntryPassword
    foreach ($catalog in @('institution-types','product-types','product-statuses','score-rules')) {
        $response = Call 'GET' "api/v1/catalogs/$catalog" $consumer.accessToken
        Check ($response.Status -eq 200 -and $response.Data.Count -gt 0) "Catalogo disponible: $catalog"
    }
    Check ((Call 'GET' 'api/v1/catalogs/roles' $admin.accessToken).Status -eq 200) 'Admin consulta roles'
    Check ((Call 'GET' 'api/v1/catalogs/roles' $entry.accessToken).Status -eq 403) 'DATA_ENTRY no consulta roles administrativos'
    foreach ($test in @(
        @{ Path='api/v1/persons'; Token=''; Code='AUTHENTICATION_REQUIRED' },
        @{ Path='api/v1/persons'; Token='invalid-token'; Code='INVALID_TOKEN' },
        @{ Path='api/v1/audit-logs'; Token=$consumer.accessToken; Code='INSUFFICIENT_PERMISSIONS' },
        @{ Path='api/v1/credit-reports/123'; Token=$consumer.accessToken; Code='INVALID_DATA' },
        @{ Path='api/v1/credit-reports/99999999999'; Token=$consumer.accessToken; Code='PERSON_NOT_FOUND' }
    )) {
        $response = Call 'GET' $test.Path $test.Token
        Check ($response.Status -ge 400 -and $response.Data.code -eq $test.Code -and $response.Data.traceId) "Error diferenciado: $($test.Code)"
    }
    $badLogin = Call 'POST' 'api/v1/auth/login' '' @{ username="inexistente-$([Guid]::NewGuid().ToString('N'))";password='NoEsUnaClaveReal_2026!'}
    Check ($badLogin.Status -eq 401 -and $badLogin.Data.code -eq 'INVALID_CREDENTIALS') 'Credenciales incorrectas diferenciadas'
    # Firma un token ya vencido para comprobar la diferencia con un token inválido.
    # La clave se lee localmente y nunca se imprime ni se guarda en los resultados.
    $config = @{}
    foreach ($line in Get-Content -LiteralPath $ApiEnvPath) {
        if ($line -match '^\s*(Jwt__Key|Jwt__Issuer|Jwt__Audience)\s*=(.*)$') {
            $config[$matches[1]] = $matches[2].Trim().Trim('"').Trim("'")
        }
    }
    foreach ($key in @('Jwt__Key','Jwt__Issuer','Jwt__Audience')) {
        $environmentValue = [Environment]::GetEnvironmentVariable($key)
        if ($environmentValue) { $config[$key] = $environmentValue }
    }
    if (-not $config['Jwt__Key']) { throw 'Configura Jwt__Key para probar el token vencido' }
    function Base64Url([byte[]]$Bytes) { [Convert]::ToBase64String($Bytes).TrimEnd('=').Replace('+','-').Replace('/','_') }
    $header = Base64Url ([Text.Encoding]::UTF8.GetBytes('{"alg":"HS256","typ":"JWT"}'))
    $claims = @{sub=[string]$consumer.account.id;role='CONSUMER';iss=$config['Jwt__Issuer'];aud=$config['Jwt__Audience'];exp=[DateTimeOffset]::UtcNow.AddHours(-1).ToUnixTimeSeconds();nbf=[DateTimeOffset]::UtcNow.AddHours(-2).ToUnixTimeSeconds()} | ConvertTo-Json -Compress
    $payload = Base64Url ([Text.Encoding]::UTF8.GetBytes($claims))
    $hmac = [Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($config['Jwt__Key']))
    try { $signature = Base64Url ($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes("$header.$payload"))) } finally { $hmac.Dispose() }
    $expired = Call 'GET' 'api/v1/auth/me' "$header.$payload.$signature"
    Check ($expired.Status -eq 401 -and $expired.Data.code -eq 'TOKEN_EXPIRED') 'Token vencido diferenciado'

    $suffix = [Guid]::NewGuid().ToString('N').Substring(0,10)
    $temporaryPassword = 'Prueba_Local_2026!'
    $temporaryAccount = Call 'POST' 'api/v1/accounts' $admin.accessToken @{institutionId=$entry.account.institutionId;roleCode='CONSUMER';username="rf09.$suffix";email="rf09.$suffix@snic.local";displayName='Prueba RF09';password=$temporaryPassword}
    Check ($temporaryAccount.Status -eq 201) 'Cuenta ficticia para probar desactivacion'
    $temporaryLogin = Login $temporaryAccount.Data.username $temporaryPassword
    $disabled = Call 'PATCH' "api/v1/accounts/$($temporaryAccount.Data.id)/status" $admin.accessToken @{isActive=$false;reason='Verificacion RF09'}
    Check ($disabled.Status -eq 200) 'Cuenta ficticia desactivada'
    $oldToken = Call 'GET' 'api/v1/auth/me' $temporaryLogin.accessToken
    Check ($oldToken.Status -eq 401 -and $oldToken.Data.code -eq 'ACCOUNT_UNAVAILABLE') 'Desactivacion invalida token vigente'
    $disabledLogin = Call 'POST' 'api/v1/auth/login' '' @{username=$temporaryAccount.Data.username;password=$temporaryPassword}
    Check ($disabledLogin.Status -eq 401 -and $disabledLogin.Data.code -eq 'INVALID_CREDENTIALS') 'Cuenta inactiva no inicia sesion'
    $invalidPerson = Call 'POST' 'api/v1/persons' $entry.accessToken @{}
    Check ($invalidPerson.Status -eq 400 -and $invalidPerson.Data.code -eq 'INVALID_DATA' -and $null -ne $invalidPerson.Data.errors) 'Validacion incluye campos y codigo'

    $nationalId = '8' + (Get-Random -Minimum 100000000 -Maximum 999999999).ToString() + (Get-Random -Minimum 0 -Maximum 10).ToString()
    $person = Call 'POST' 'api/v1/persons' $entry.accessToken @{nationalId=$nationalId;firstName='Indicadores';middleName=$null;firstSurname='Prueba';secondSurname=$null;birthDate='1990-01-01'}
    Check ($person.Status -eq 201) 'Persona ficticia creada'
    $empty = Call 'GET' "api/v1/credit-reports/$nationalId" $consumer.accessToken
    Check ($empty.Status -eq 200 -and $empty.Data.code -eq 'NO_CREDIT_HISTORY' -and $null -eq $empty.Data.indicators.score -and $empty.Data.indicators.isSimulated) 'Sin historial: score nulo y simulacion explicita'

    $cases = @(@(0,100,'LOW'),@(1,80,'LOW'),@(30,80,'LOW'),@(31,60,'MEDIUM'),@(60,60,'MEDIUM'),@(61,40,'HIGH'),@(90,40,'HIGH'),@(91,20,'HIGH'))
    foreach ($case in $cases) {
        $product = Call 'POST' 'api/v1/credit-products' $entry.accessToken @{
            personId=$person.Data.id;productTypeCode='LOAN';productStatusCode='OPEN'
            externalProductIdentifier="IND-$nationalId-$($case[0])";approvedAmount=1000;currentBalance=500;currencyCode='DOP'
            openingDate='2025-01-01';closingDate=$null;latestReportDate='2025-02-01';latestDaysPastDue=$case[0]
        }
        Check ($product.Status -eq 201) "Producto con atraso $($case[0]) registrado"
        $report = Call 'GET' "api/v1/credit-reports/$nationalId" $entry.accessToken
        $indicator = $report.Data.indicators
        Check ($report.Status -eq 200 -and $indicator.score -eq $case[1] -and $indicator.riskLevel -eq $case[2] -and $indicator.estimatedDefaultPercentage -eq (100-$case[1]) -and $indicator.isSimulated) "Atraso $($case[0]): score $($case[1]), riesgo $($case[2])"
    }
    $auditPath = "api/v1/audit-logs?from=$([Uri]::EscapeDataString($started))&accountId=$($entry.account.id)&institutionId=$($entry.account.institutionId)&personId=$($person.Data.id)&pageSize=100"
    $logs = Call 'GET' $auditPath $admin.accessToken
    Check ($logs.Status -eq 200 -and $logs.Data.totalCount -ge 16) 'Auditoria filtra fecha, cuenta, institucion y persona'
    Check (@($logs.Data.items | Where-Object {$_.operation -eq 'CREDITREPORTS_GETBYNATIONALID' -and $_.result -eq 'SUCCESS'}).Count -ge 8) 'Consultas consolidadas auditadas'
    Check (@($logs.Data.items | Where-Object {$_.operation -eq 'CREDITPRODUCTS_CREATE'}).Count -eq 8) 'Creaciones auditadas'
    $denied = Call 'GET' "api/v1/audit-logs?accountId=$($consumer.account.id)&result=DENIED&pageSize=100" $admin.accessToken
    Check ($denied.Status -eq 200 -and $denied.Data.totalCount -gt 0) 'Accesos rechazados auditados'
    Check ((Call 'GET' 'api/v1/audit-logs?pageSize=101' $admin.accessToken).Status -eq 400) 'Tamano de pagina limitado'
    Check ((Call 'GET' 'api/v1/audit-logs?from=2026-12-31T00:00:00Z&to=2026-01-01T00:00:00Z' $admin.accessToken).Status -eq 400) 'Rango de fechas validado'
    $rawLogs = ConvertTo-Json -InputObject $logs.Data -Depth 20 -Compress
    Check ($rawLogs -notmatch 'accessToken|passwordHash|Jwt__Key|Bearer ') 'Auditoria no expone secretos ni tokens'
    Write-Output "Comprobaciones correctas: $script:Checks; persona de demostracion: $nationalId"
} finally { $client.Dispose() }
