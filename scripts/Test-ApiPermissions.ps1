param(
    [string]$BaseUrl = 'http://localhost:5041',
    [string]$AdminUsername = 'admin',
    [string]$AdminPassword = 'Admin_Local_2026!',
    [string]$ConsumerUsername = 'consumer.demo',
    [string]$ConsumerPassword = 'Consumer_Local_2026!',
    [string]$EntryUsername = 'dataentry.demo',
    [string]$EntryPassword = 'DataEntry_Local_2026!'
)

# Prueba de integración para una base de demostración con los tres usuarios.
# Crea una persona y un producto ficticios y conserva sus registros y auditoría.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$client = New-Object System.Net.Http.HttpClient
$client.BaseAddress = [Uri]($BaseUrl.TrimEnd('/') + '/')
$script:Checks = 0

function Invoke-Api([string]$Method, [string]$Path, [string]$Token, $Body = $null) {
    $request = New-Object System.Net.Http.HttpRequestMessage
    $request.Method = New-Object System.Net.Http.HttpMethod($Method)
    $request.RequestUri = [Uri]::new($Path.TrimStart('/'), [UriKind]::Relative)
    if ($Token) {
        $request.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Bearer', $Token)
    }
    if ($null -ne $Body) {
        $json = ConvertTo-Json -InputObject $Body -Depth 10 -Compress
        $request.Content = New-Object System.Net.Http.StringContent($json, [Text.Encoding]::UTF8, 'application/json')
    }
    try {
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        try {
            $raw = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            $data = if ($raw -and $response.Content.Headers.ContentType.MediaType -like '*json*') { ConvertFrom-Json -InputObject $raw } else { $null }
            return [pscustomobject]@{ Status = [int]$response.StatusCode; Data = $data }
        } finally { $response.Dispose() }
    } finally { $request.Dispose() }
}

function Assert-Status($Response, [int]$Expected, [string]$Label) {
    if ($Response.Status -ne $Expected) { throw "$Label : esperado $Expected, recibido $($Response.Status)" }
    $script:Checks++
    Write-Output "OK $Expected - $Label"
}

function Get-Login([string]$Username, [string]$Password, [string]$Role) {
    $response = Invoke-Api 'POST' 'api/v1/auth/login' '' @{ username = $Username; password = $Password }
    Assert-Status $response 200 "Login $Role" | Out-Host
    if ($response.Data.account.roleCode -ne $Role) { throw "Rol inesperado para $Username" }
    return $response.Data
}

function Product-Body($Product) {
    return @{
        personId = $Product.personId
        productTypeCode = $Product.productTypeCode
        productStatusCode = $Product.productStatusCode
        externalProductIdentifier = $Product.externalProductIdentifier
        approvedAmount = $Product.approvedAmount
        currentBalance = $Product.currentBalance
        currencyCode = $Product.currencyCode
        openingDate = $Product.openingDate
        closingDate = $Product.closingDate
        latestReportDate = $Product.latestReportDate
        latestDaysPastDue = $Product.latestDaysPastDue
    }
}

try {
    $admin = Get-Login $AdminUsername $AdminPassword 'ADMINISTRATOR'
    $consumer = Get-Login $ConsumerUsername $ConsumerPassword 'CONSUMER'
    $entry = Get-Login $EntryUsername $EntryPassword 'DATA_ENTRY'

    foreach ($login in @($admin, $consumer, $entry)) {
        foreach ($path in @('auth/me', 'institutions', 'persons', 'credit-products')) {
            Assert-Status (Invoke-Api 'GET' "api/v1/$path" $login.accessToken) 200 "$($login.account.roleCode) consulta $path"
        }
    }

    foreach ($login in @($consumer, $entry)) {
        Assert-Status (Invoke-Api 'GET' 'api/v1/accounts' $login.accessToken) 403 "$($login.account.roleCode) no lista cuentas"
        Assert-Status (Invoke-Api 'POST' 'api/v1/accounts' $login.accessToken @{}) 403 "$($login.account.roleCode) no crea cuentas"
        Assert-Status (Invoke-Api 'POST' 'api/v1/institutions' $login.accessToken @{}) 403 "$($login.account.roleCode) no crea instituciones"
        Assert-Status (Invoke-Api 'PATCH' 'api/v1/accounts/1/status' $login.accessToken @{isActive = $false}) 403 "$($login.account.roleCode) no cambia estados de cuentas"
    }
    Assert-Status (Invoke-Api 'GET' 'api/v1/accounts' $admin.accessToken) 200 'ADMINISTRATOR lista cuentas'
    Assert-Status (Invoke-Api 'POST' 'api/v1/accounts' $admin.accessToken @{}) 400 'ADMINISTRATOR llega a validar nuevas cuentas'
    Assert-Status (Invoke-Api 'POST' 'api/v1/institutions' $admin.accessToken @{}) 400 'ADMINISTRATOR llega a validar nuevas instituciones'

    $suffix = [Guid]::NewGuid().ToString('N').Substring(0, 10)
    $nationalId = '9' + (Get-Random -Minimum 100000000 -Maximum 999999999).ToString() + (Get-Random -Minimum 0 -Maximum 10).ToString()
    $personBody = @{ nationalId = $nationalId; firstName = 'Prueba'; middleName = $null; firstSurname = 'Permisos'; secondSurname = $null; birthDate = '1990-01-01' }
    foreach ($login in @($admin, $consumer)) {
        Assert-Status (Invoke-Api 'POST' 'api/v1/persons' $login.accessToken $personBody) 403 "$($login.account.roleCode) no registra personas"
    }
    $person = Invoke-Api 'POST' 'api/v1/persons' $entry.accessToken $personBody
    Assert-Status $person 201 'DATA_ENTRY registra persona'
    foreach ($login in @($consumer, $entry)) {
        Assert-Status (Invoke-Api 'PUT' "api/v1/persons/$($person.Data.id)" $login.accessToken $personBody) 403 "$($login.account.roleCode) no corrige personas"
    }
    $personBody.firstName = 'Prueba corregida'
    Assert-Status (Invoke-Api 'PUT' "api/v1/persons/$($person.Data.id)" $admin.accessToken $personBody) 200 'ADMINISTRATOR corrige persona registrada por DATA_ENTRY'

    $productBody = @{
        personId = $person.Data.id; productTypeCode = 'LOAN'; productStatusCode = 'OPEN'
        externalProductIdentifier = "PERMISOS-$suffix"; approvedAmount = 1000; currentBalance = 500
        currencyCode = 'DOP'; openingDate = '2025-01-01'; closingDate = $null
        latestReportDate = '2025-02-01'; latestDaysPastDue = 0
    }
    foreach ($login in @($admin, $consumer)) {
        Assert-Status (Invoke-Api 'POST' 'api/v1/credit-products' $login.accessToken $productBody) 403 "$($login.account.roleCode) no crea productos"
    }
    $product = Invoke-Api 'POST' 'api/v1/credit-products' $entry.accessToken $productBody
    Assert-Status $product 201 'DATA_ENTRY crea producto sin enviar institutionId'
    if ($product.Data.institutionId -ne $entry.account.institutionId) { throw 'La API no asigno la institucion de la cuenta' }
    $productBody.currentBalance = 400
    Assert-Status (Invoke-Api 'PUT' "api/v1/credit-products/$($product.Data.id)" $entry.accessToken $productBody) 200 'DATA_ENTRY corrige producto propio sin enviar institutionId'
    foreach ($login in @($admin, $consumer)) {
        Assert-Status (Invoke-Api 'PUT' "api/v1/credit-products/$($product.Data.id)" $login.accessToken $productBody) 403 "$($login.account.roleCode) no corrige productos"
    }

    $institutions = Invoke-Api 'GET' 'api/v1/institutions' $admin.accessToken
    $otherInstitution = $institutions.Data | Where-Object { $_.id -ne $entry.account.institutionId -and $_.isActive } | Select-Object -First 1
    if (-not $otherInstitution) { throw 'La prueba de aislamiento necesita otra institucion activa' }
    $productBody.institutionId = $otherInstitution.id
    Assert-Status (Invoke-Api 'POST' 'api/v1/credit-products' $entry.accessToken $productBody) 403 'No permite crear productos para otra institucion'
    Assert-Status (Invoke-Api 'PUT' "api/v1/credit-products/$($product.Data.id)" $entry.accessToken $productBody) 403 'No permite trasladar productos propios a otra institucion'
    $products = Invoke-Api 'GET' 'api/v1/credit-products' $admin.accessToken
    $foreign = $products.Data | Where-Object { $_.institutionId -eq $otherInstitution.id } | Select-Object -First 1
    if (-not $foreign) { throw 'La prueba de aislamiento necesita un producto de otra institucion' }
    $foreignBody = Product-Body $foreign
    Assert-Status (Invoke-Api 'PUT' "api/v1/credit-products/$($foreign.id)" $entry.accessToken $foreignBody) 403 'No permite corregir producto ajeno omitiendo institutionId'
    $foreignBody.institutionId = $entry.account.institutionId
    Assert-Status (Invoke-Api 'PUT' "api/v1/credit-products/$($foreign.id)" $entry.accessToken $foreignBody) 403 'No permite apropiarse de producto ajeno enviando la institucion propia'

    foreach ($login in @($admin, $consumer, $entry)) {
        foreach ($path in @("institutions/$($entry.account.institutionId)", "persons/$($person.Data.id)", "credit-products/$($product.Data.id)", "credit-products/$($foreign.id)", "credit-reports/$nationalId")) {
            Assert-Status (Invoke-Api 'GET' "api/v1/$path" $login.accessToken) 200 "$($login.account.roleCode) consulta $path"
        }
    }
    Assert-Status (Invoke-Api 'GET' 'api/v1/persons' '') 401 'Consulta sin token rechazada'
    Write-Output "Comprobaciones correctas: $script:Checks. Persona demo: $nationalId. Producto demo: $($product.Data.id)."
} finally { $client.Dispose() }
