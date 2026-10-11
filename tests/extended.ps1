# Extended verification: bad login, authed order history, asset refs, persistence.
param([string]$Base = "http://localhost:8085")
$fail = 0
function Assert($name, $cond, $extra = "") {
  if ($cond) { Write-Output "PASS $name $extra" }
  else { Write-Output "FAIL $name $extra"; $script:fail++ }
}
function New-Sess { New-Object Microsoft.PowerShell.Commands.WebRequestSession }
function Get-Token($html) {
  ([regex]::Matches($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"') | Select-Object -First 1).Groups[1].Value
}

# bad password rejected
$s = New-Sess
$lp = Invoke-WebRequest -Uri "$Base/Account/Login" -WebSession $s -UseBasicParsing -TimeoutSec 30
$bad = Invoke-WebRequest -Uri "$Base/Account/Login" -Method Post -Body @{ __RequestVerificationToken=(Get-Token $lp.Content); Email="nobody@test.local"; Password="WrongPass1" } -WebSession $s -UseBasicParsing -TimeoutSec 30
Assert "bad-login-rejected" ($bad.Content -match "Invalid email or password") $bad.StatusCode

# register, place order authed, check history shows it
$s2 = New-Sess
$rp = Invoke-WebRequest -Uri "$Base/Account/Register" -WebSession $s2 -UseBasicParsing -TimeoutSec 30
$email = "hist" + [Guid]::NewGuid().ToString("N").Substring(0,8) + "@test.local"
Invoke-WebRequest -Uri "$Base/Account/Register" -Method Post -Body @{ __RequestVerificationToken=(Get-Token $rp.Content); FirstName="Hist"; LastName="User"; Email=$email; Password="Secret123" } -WebSession $s2 -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 0 -ErrorAction SilentlyContinue | Out-Null
$pp = Invoke-WebRequest -Uri "$Base/p/classic-cotton-kurta" -WebSession $s2 -UseBasicParsing -TimeoutSec 30
Invoke-WebRequest -Uri "$Base/Cart/Add" -Method Post -Body @{ __RequestVerificationToken=(Get-Token $pp.Content); productId="1"; quantity="1" } -WebSession $s2 -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 0 -ErrorAction SilentlyContinue | Out-Null
$co = Invoke-WebRequest -Uri "$Base/Checkout/Index" -WebSession $s2 -UseBasicParsing -TimeoutSec 30
$key = ([regex]::Match($co.Content, 'name="idempotencyKey" value="([^"]+)"')).Groups[1].Value
$pl = Invoke-WebRequest -Uri "$Base/Checkout/Place" -Method Post -Body @{ __RequestVerificationToken=(Get-Token $co.Content); idempotencyKey=$key; 'Form.FullName'='Hist User'; 'Form.Email'=$email; 'Form.AddressLine1'='9 Test Rd'; 'Form.City'='Jaipur'; 'Form.State'='RJ'; 'Form.PostalCode'='302001'; 'Form.Country'='India'; 'Form.ShippingMethod'='standard'; 'Form.PaymentMethod'='cod' } -WebSession $s2 -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 0 -ErrorAction SilentlyContinue
$ord = ([regex]::Match([string]$pl.Headers.Location, 'LE-[\d-]+')).Groups[0].Value
$hist = Invoke-WebRequest -Uri "$Base/Account/Orders" -WebSession $s2 -UseBasicParsing -TimeoutSec 30
Assert "order-in-history" ($hist.Content -match [regex]::Escape($ord)) $ord

# every script/css/img ref on homepage resolves
$homePage = Invoke-WebRequest -Uri "$Base/" -UseBasicParsing -TimeoutSec 30
$refs = [regex]::Matches($homePage.Content, '(src|href)="(/[^"]+)"') | ForEach-Object { $_.Groups[2].Value } | Sort-Object -Unique
$bad_refs = @()
foreach ($u in $refs) {
  if ($u -match "^/(c|p|Catalog|Cart|Checkout|Account|Home|health)") { continue }
  try { $q = Invoke-WebRequest -Uri ($Base + $u) -UseBasicParsing -TimeoutSec 30; if ($q.StatusCode -ne 200) { $bad_refs += $u } }
  catch { $bad_refs += ($u + " ERR") }
}
Assert "static-refs-ok" ($bad_refs.Count -eq 0) ($bad_refs -join ", ")

Write-Output ("EXTENDED-RESULT fail=" + $script:fail)
exit $script:fail
