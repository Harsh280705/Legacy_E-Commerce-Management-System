# LegacyEcommerce smoke tests (also the deliverable test script).
# Usage: powershell -ExecutionPolicy Bypass -File tests/smoke.ps1 [-Base http://localhost:8085]
param([string]$Base = "http://localhost:8085")
$ErrorActionPreference = "Continue"
$script:fail = 0
function Assert($name, $cond, $extra = "") {
  if ($cond) { Write-Output "PASS $name $extra" }
  else { Write-Output "FAIL $name $extra"; $script:fail++ }
}
function New-Sess { New-Object Microsoft.PowerShell.Commands.WebRequestSession }
function Get-Page($s, $url) {
  Invoke-WebRequest -Uri "$Base$url" -WebSession $s -UseBasicParsing -TimeoutSec 30
}
function Get-Token($html) {
  ([regex]::Matches($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"') | Select-Object -First 1).Groups[1].Value
}
function Post-NoRedir($s, $url, $body) {
  Invoke-WebRequest -Uri "$Base$url" -Method Post -Body $body -WebSession $s -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 0 -ErrorAction SilentlyContinue
}

# --- anonymous routes ---
$r = Get-Page (New-Sess) "/health"; Assert "health" ($r.Content -eq "OK") $r.StatusCode
$r = Get-Page (New-Sess) "/"; Assert "home" ($r.StatusCode -eq 200 -and $r.Content -match "Yantra") $r.RawContentLength
$r = Get-Page (New-Sess) "/Catalog/Index"; Assert "catalog" ($r.Content -match "product") $r.StatusCode
$r = Get-Page (New-Sess) "/c/men-fashion"; Assert "category" ($r.Content -match "Men Fashion") $r.StatusCode
$r = Get-Page (New-Sess) "/p/classic-cotton-kurta"; Assert "product" ($r.Content -match "Add to cart") $r.StatusCode
$r = Get-Page (New-Sess) "/Catalog/Grid?q=shoes"; Assert "grid-ajax" ($r.StatusCode -eq 200) $r.RawContentLength

# --- cart flow ---
$s = New-Sess
$p = Get-Page $s "/p/classic-cotton-kurta"; $tok = Get-Token $p.Content
$add = Post-NoRedir $s "/Cart/Add" @{ __RequestVerificationToken=$tok; productId="1"; quantity="2" }
Assert "cart-add-redirect-or-json" ($add.StatusCode -in @(200,302)) $add.StatusCode
$c = Get-Page $s "/Cart/Count"; Assert "cart-count-2" ($c.Content -match '"count":2') $c.Content

# --- guest checkout ---
$co = Get-Page $s "/Checkout/Index"
$ctok = Get-Token $co.Content
$key = ([regex]::Match($co.Content, 'name="idempotencyKey" value="([^"]+)"')).Groups[1].Value
Assert "checkout-form" ($co.StatusCode -eq 200 -and $key.Length -gt 10) $co.StatusCode
$place = Post-NoRedir $s "/Checkout/Place" @{ __RequestVerificationToken=$ctok; idempotencyKey=$key; 'Form.FullName'='Smoke Test'; 'Form.Email'='smoke@test.local'; 'Form.AddressLine1'='1 Test St'; 'Form.City'='Pune'; 'Form.State'='MH'; 'Form.PostalCode'='411001'; 'Form.Country'='India'; 'Form.ShippingMethod'='standard'; 'Form.PaymentMethod'='cod' }
$loc = [string]$place.Headers.Location
Assert "order-placed" ($place.StatusCode -eq 302 -and $loc -match "Success") "$($place.StatusCode) $loc"
$succ = Get-Page $s $loc; Assert "order-success" ($succ.Content -match "Order placed") $succ.StatusCode

# --- auth: register/login/profile/orders/logoff ---
$s2 = New-Sess
$u = Post-NoRedir $s2 "/Account/Profile" @{}
Assert "profile-requires-auth" ($u.StatusCode -eq 302) $u.StatusCode
$email = "smoke" + [Guid]::NewGuid().ToString("N").Substring(0,8) + "@test.local"
$rp = Get-Page $s2 "/Account/Register"; $rtok = Get-Token $rp.Content
$reg = Post-NoRedir $s2 "/Account/Register" @{ __RequestVerificationToken=$rtok; FirstName="Smoke"; LastName="Test"; Email=$email; Password="Secret123" }
Assert "register-redirect" ($reg.StatusCode -eq 302) "$($reg.StatusCode) $($reg.Headers.Location)"
$prof = Get-Page $s2 "/Account/Profile"
Assert "profile-authed" ($prof.Content -match "My account" -and $prof.Content -match [regex]::Escape($email)) $prof.StatusCode
$lp = Get-Page $s2 "/Account/Profile"; $ltok = Get-Token $lp.Content
Post-NoRedir $s2 "/Account/LogOff" @{ __RequestVerificationToken=$ltok } | Out-Null
$lip = Get-Page $s2 "/Account/Login"; $litok = Get-Token $lip.Content
$login = Post-NoRedir $s2 "/Account/Login" @{ __RequestVerificationToken=$litok; Email=$email; Password="Secret123" }
Assert "login-redirect" ($login.StatusCode -eq 302) "$($login.StatusCode) $($login.Headers.Location)"
$prof2 = Get-Page $s2 "/Account/Profile"
Assert "profile-after-login" ($prof2.Content -match "My account") $prof2.StatusCode

Write-Output ("SMOKE-RESULT fail=" + $script:fail)
exit $script:fail
