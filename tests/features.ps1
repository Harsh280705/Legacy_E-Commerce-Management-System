# LegacyEcommerce feature tests.
# Covers: pagination, search/sort, infinite scroll, cart AJAX, checkout
# confirmation + idempotent confirm, and admin authorisation.
# Usage: powershell -ExecutionPolicy Bypass -File tests/features.ps1 [-Base http://localhost:8085]
param(
  [string]$Base = "http://localhost:8085",
  [string]$AdminEmail = "admin@localhost.local",
  # No default password: pass -AdminPassword explicitly for local runs
  # (keeps real credentials out of version control).
  [string]$AdminPassword = ""
)
$ErrorActionPreference = "Continue"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$script:fail = 0

function Assert($name, $cond, $extra = "") {
  if ($cond) { Write-Output "PASS $name $extra" }
  else { Write-Output "FAIL $name $extra"; $script:fail++ }
}
function New-Sess { New-Object Microsoft.PowerShell.Commands.WebRequestSession }
function Get-Page($s, $url) {
  Invoke-WebRequest -Uri "$Base$url" -WebSession $s -UseBasicParsing -TimeoutSec 45
}
function Get-Token($html) {
  ([regex]::Matches([string]$html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"') | Select-Object -First 1).Groups[1].Value
}
function Post-NoRedir($s, $url, $body, $ajax = $false) {
  $h = @{}
  if ($ajax) { $h["X-Requested-With"] = "XMLHttpRequest" }
  Invoke-WebRequest -Uri "$Base$url" -Method Post -Body $body -Headers $h -WebSession $s -UseBasicParsing -TimeoutSec 45 -MaximumRedirection 0 -ErrorAction SilentlyContinue
}
function Post-Ajax($s, $url, $body) {
  $h = @{ "X-Requested-With" = "XMLHttpRequest" }
  (Invoke-WebRequest -Uri "$Base$url" -Method Post -Body $body -Headers $h -WebSession $s -UseBasicParsing -TimeoutSec 45).Content | ConvertFrom-Json
}
function Get-Grid($qs) {
  (Invoke-WebRequest -Uri "$Base/Catalog/GridData?$qs" -UseBasicParsing -TimeoutSec 45).Content | ConvertFrom-Json
}
function Get-Pids($html) {
  @([regex]::Matches([string]$html, 'data-pid="(\d+)"') | ForEach-Object { $_.Groups[1].Value })
}
function Get-Prices($html) {
  @([regex]::Matches([string]$html, '<strong>&#8377;([0-9,\.]+)</strong>') | ForEach-Object { [decimal]($_.Groups[1].Value -replace ',', '') })
}
function Register-User($prefix) {
  $s = New-Sess
  $rp = Get-Page $s "/Account/Register"
  $email = $prefix + [Guid]::NewGuid().ToString("N").Substring(0, 8) + "@test.local"
  Post-NoRedir $s "/Account/Register" @{ __RequestVerificationToken = (Get-Token $rp.Content); FirstName = "Feat"; LastName = "User"; Email = $email; Password = "Secret123" } | Out-Null
  $s
}
function Login-User($s, $email, $password) {
  $lp = Get-Page $s "/Account/Login"
  Post-NoRedir $s "/Account/Login" @{ __RequestVerificationToken = (Get-Token $lp.Content); Email = $email; Password = $password } | Out-Null
  $s
}

Write-Output "== catalog: pagination, search, sort =="
$home = Get-Page (New-Sess) "/Catalog/Index"
Assert "catalog-total-1600" ($home.Content -match "1600 product\(s\)") ([regex]::Match($home.Content, '\d+ product\(s\)').Value)
Assert "catalog-pager-present" ($home.Content -match 'id="pager"') ""
Assert "catalog-page1-12-cards" ((Get-Pids $home.Content).Count -eq 12) ((Get-Pids $home.Content).Count)

$p1 = Get-Pids (Get-Page (New-Sess) "/Catalog/Index?page=1").Content
$p2 = Get-Pids (Get-Page (New-Sess) "/Catalog/Index?page=2").Content
$overlap = @($p1 | Where-Object { $p2 -contains $_ })
Assert "catalog-pages-disjoint" ($overlap.Count -eq 0 -and $p1.Count -eq 12 -and $p2.Count -eq 12) "overlap=$($overlap.Count)"

$p24 = Get-Pids (Get-Page (New-Sess) "/Catalog/Index?size=24").Content
Assert "catalog-page-size-24" ($p24.Count -eq 24) $p24.Count

$last = Get-Page (New-Sess) "/Catalog/Index?page=134"
Assert "catalog-last-page-clamped" ($last.StatusCode -eq 200 -and (Get-Pids $last.Content).Count -eq 4) ((Get-Pids $last.Content).Count)
$huge = Get-Page (New-Sess) "/Catalog/Index?page=9999"
Assert "catalog-invalid-page-clamped-to-last" ((Get-Pids $huge.Content).Count -eq 4) ((Get-Pids $huge.Content).Count)

$asc = Get-Prices (Get-Grid "sort=price_asc&page=1&size=12").html
Assert "catalog-sort-price-asc" (@(Compare-Object $asc (@($asc | Sort-Object))).Count -eq 0) ($asc[0])
$desc = Get-Prices (Get-Grid "sort=price_desc&page=1&size=12").html
Assert "catalog-sort-price-desc" ($desc[0] -gt $desc[1] -and $desc[0] -ge $asc[11]) ($desc[0])

$srch = Get-Page (New-Sess) "/Catalog/Index?q=kurta"
$kurtaTotal = [int]([regex]::Match($srch.Content, '([0-9,]+) product').Groups[1].Value -replace ',', '')
Assert "catalog-search-filters" ($kurtaTotal -gt 0 -and $kurtaTotal -lt 1600 -and $srch.Content -match 'for &quot;kurta&quot;') "total=$kurtaTotal"
$empty = Get-Page (New-Sess) "/Catalog/Index?q=zzzznotfound"
Assert "catalog-search-empty-state" ($empty.Content -match "yb-empty-state") ""

Write-Output "== catalog: infinite scroll =="
$inf = Get-Page (New-Sess) "/Catalog/Index?mode=infinite"
Assert "infinite-zone-present" ($inf.Content -match 'id="infinite-zone"' -and $inf.Content -match 'data-hasmore="true"') ""
Assert "infinite-no-pager" ($inf.Content -notmatch 'id="pager"') ""
$d1 = Get-Grid "page=1&size=12&sort=name_asc"
$d2 = Get-Grid "page=2&size=12&sort=name_asc"
Assert "griddata-json-shape" ($null -ne $d1.html -and $null -ne $d1.hasMore -and $null -ne $d1.nextPage -and $null -ne $d1.total) ($d1.total)
Assert "griddata-next-page" ($d1.nextPage -eq 2 -and $d2.nextPage -eq 3) ""
$gp1 = Get-Pids $d1.html; $gp2 = Get-Pids $d2.html
Assert "griddata-pages-disjoint" (@($gp1 | Where-Object { $gp2 -contains $_ }).Count -eq 0 -and $gp1.Count -eq 12) ""
$lastD = Get-Grid "page=134&size=12&sort=name_asc"
Assert "griddata-stops-at-final-page" ($lastD.hasMore -eq $false -and (Get-Pids $lastD.html).Count -eq 4) ""
$clampD = Get-Grid "page=9999&size=12"
Assert "griddata-invalid-page-clamped" ($clampD.hasMore -eq $false -and (Get-Pids $clampD.html).Count -eq 4) $clampD.total
$filt = Get-Page (New-Sess) "/Catalog/Index?q=shoes&sort=price_asc&size=24&mode=infinite"
$grid = ([regex]::Match($filt.Content, 'YB_GRID = \{[^}]+\}')).Value
Assert "infinite-preserves-filters" ($grid -match "q: 'shoes'" -and $grid -match "sort: 'price_asc'" -and $grid -match "size: 24") $grid

Write-Output "== cart: ajax add / update / remove / stock / variant =="
$s = New-Sess
$pp = Get-Page $s "/p/classic-cotton-kurta"
$tok = Get-Token $pp.Content
$a1 = Post-Ajax $s "/Cart/Add" @{ __RequestVerificationToken = $tok; productId = "1"; quantity = "2" }
Assert "ajax-add-success" ($a1.success -eq $true -and $a1.count -eq 2) "msg=$($a1.message) count=$($a1.count)"
$a2 = Post-Ajax $s "/Cart/Add" @{ __RequestVerificationToken = $tok; productId = "1"; quantity = "1" }
Assert "ajax-add-merges-line" ($a2.success -eq $true -and $a2.count -eq 3) "count=$($a2.count)"
$a3 = Post-Ajax $s "/Cart/Add" @{ __RequestVerificationToken = $tok; productId = "999999"; quantity = "1" }
Assert "ajax-add-unknown-product-fails" ($a3.success -eq $false -and $a3.message -match "not found") $a3.message
$a4 = Post-Ajax $s "/Cart/Add" @{ __RequestVerificationToken = $tok; productId = "1117"; quantity = "5" }
Assert "ajax-add-stock-limit-fails" ($a4.success -eq $false -and $a4.message -match "available") $a4.message
$a5 = Post-Ajax $s "/Cart/Add" @{ __RequestVerificationToken = $tok; productId = "1"; variantId = "1"; quantity = "1" }
Assert "ajax-add-variant-success" ($a5.success -eq $true -and $a5.count -eq 4) "count=$($a5.count)"
$cart = Get-Page $s "/Cart/Index"
Assert "cart-variant-line-label" ($cart.Content -match "Size: S") ""
$lineId = ([regex]::Match($cart.Content, 'data-line="(\d+)"')).Groups[1].Value
Assert "cart-line-id-extracted" ($lineId.Length -gt 0) $lineId

$upd = Invoke-WebRequest -Uri "$Base/Cart/Update" -Method Post -Body @{ __RequestVerificationToken = (Get-Token $cart.Content); cartItemId = $lineId; quantity = "99" } -Headers @{ "X-Requested-With" = "XMLHttpRequest" } -WebSession $s -UseBasicParsing -TimeoutSec 45
Assert "ajax-update-stock-capped" ($upd.Content -match 'data-ok="false"' -and $upd.Content -match "available") ([regex]::Match($upd.Content, 'data-message="[^"]*"').Value)
$upd2 = Invoke-WebRequest -Uri "$Base/Cart/Update" -Method Post -Body @{ __RequestVerificationToken = (Get-Token $cart.Content); cartItemId = $lineId; quantity = "0" } -Headers @{ "X-Requested-With" = "XMLHttpRequest" } -WebSession $s -UseBasicParsing -TimeoutSec 45
Assert "ajax-update-zero-removes" ($upd2.Content -match 'data-message="Removed"') ""
$cnt = (Invoke-WebRequest -Uri "$Base/Cart/Count" -WebSession $s -UseBasicParsing -TimeoutSec 45).Content | ConvertFrom-Json
Assert "cart-count-after-removal" ($cnt.count -eq 3) $cnt.count
$mini = Get-Page $s "/Cart/Mini"
Assert "minicart-partial" ($mini.StatusCode -eq 200 -and $mini.Content -match "Mini cart") $mini.StatusCode

$s2 = New-Sess
$pp2 = Get-Page $s2 "/p/classic-cotton-kurta"
Post-Ajax $s2 "/Cart/Add" @{ __RequestVerificationToken = (Get-Token $pp2.Content); productId = "1"; quantity = "1" } | Out-Null
$cart2 = Get-Page $s2 "/Cart/Index"
$rm = Invoke-WebRequest -Uri "$Base/Cart/Remove" -Method Post -Body @{ __RequestVerificationToken = (Get-Token $cart2.Content); cartItemId = ([regex]::Match($cart2.Content, 'data-line="(\d+)"')).Groups[1].Value } -Headers @{ "X-Requested-With" = "XMLHttpRequest" } -WebSession $s2 -UseBasicParsing -TimeoutSec 45
Assert "ajax-remove-empty-state" ($rm.Content -match "yb-empty-state") ""

Write-Output "== checkout: review step + confirm idempotency =="
$s3 = New-Sess
$pp3 = Get-Page $s3 "/p/classic-cotton-kurta"
Post-Ajax $s3 "/Cart/Add" @{ __RequestVerificationToken = (Get-Token $pp3.Content); productId = "1"; quantity = "1" } | Out-Null
$co = Get-Page $s3 "/Checkout/Index"
$key = ([regex]::Match($co.Content, 'name="idempotencyKey" value="([^"]+)"')).Groups[1].Value
Assert "checkout-idempotency-key-issued" ($key.Length -gt 10) $key
$place = Post-NoRedir $s3 "/Checkout/Place" @{ __RequestVerificationToken = (Get-Token $co.Content); idempotencyKey = $key; 'Form.FullName' = 'Feat Checkout'; 'Form.Email' = 'feat@test.local'; 'Form.AddressLine1' = '2 Test Rd'; 'Form.City' = 'Jaipur'; 'Form.State' = 'RJ'; 'Form.PostalCode' = '302001'; 'Form.Country' = 'India'; 'Form.ShippingMethod' = 'standard'; 'Form.PaymentMethod' = 'cod' }
Assert "checkout-place-redirects-to-review" ($place.StatusCode -eq 302 -and [string]$place.Headers.Location -match "Review") "$($place.StatusCode) $($place.Headers.Location)"
$review = Get-Page $s3 "/Checkout/Review"
Assert "checkout-review-renders" ($review.StatusCode -eq 200 -and $review.Content -match "Review") $review.StatusCode
Assert "checkout-review-has-server-total" ($review.Content -match "Grand total|Grand Total|Total:") ""
$confirmTok = Get-Token $review.Content
$conf = Post-NoRedir $s3 "/Checkout/Confirm" @{ __RequestVerificationToken = $confirmTok }
Assert "checkout-confirm-redirects-to-success" ($conf.StatusCode -eq 302 -and [string]$conf.Headers.Location -match "Success") "$($conf.StatusCode) $($conf.Headers.Location)"
$orderNumber = ([regex]::Match([string]$conf.Headers.Location, 'LE-[0-9a-f\-]+')).Value
Assert "checkout-order-number-format" ($orderNumber -match "^LE-[0-9a-f\-]{36}$") $orderNumber
$succ = Get-Page $s3 ("/Checkout/Success?orderNumber=" + $orderNumber)
Assert "checkout-success-shows-order" ($succ.Content -match "Order placed" -and $succ.Content -match [regex]::Escape($orderNumber)) ""
$conf2 = Post-NoRedir $s3 "/Checkout/Confirm" @{ __RequestVerificationToken = $confirmTok }
Assert "checkout-double-confirm-no-duplicate" ($conf2.StatusCode -eq 302 -and [string]$conf2.Headers.Location -notmatch "Success") "$($conf2.StatusCode) $($conf2.Headers.Location)"
$after = Get-Page $s3 "/Cart/Index"
Assert "checkout-cart-cleared-after-order" ($after.Content -match "yb-empty-state") ""

Write-Output "== admin: authorisation =="
$anon = Invoke-WebRequest -Uri "$Base/Admin" -UseBasicParsing -TimeoutSec 45 -MaximumRedirection 0 -ErrorAction SilentlyContinue
Assert "admin-anonymous-redirects-to-login" ($anon.StatusCode -eq 302 -and [string]$anon.Headers.Location -match "Account/Login") "$($anon.StatusCode) $($anon.Headers.Location)"

$plain = Register-User "plain"
$na = Get-Page $plain "/Admin"
Assert "admin-non-admin-403" ($na.StatusCode -eq 403) $na.StatusCode
$na2 = Get-Page $plain "/Admin/Products"
Assert "admin-non-admin-products-403" ($na2.StatusCode -eq 403) $na2.StatusCode
$naSave = Post-NoRedir $plain "/Admin/SaveProduct" @{ id = "0"; name = "x" }
Assert "admin-non-admin-save-403" ($naSave.StatusCode -eq 403) $naSave.StatusCode

$adm = New-Sess
Login-User $adm $AdminEmail $AdminPassword
$dash = Get-Page $adm "/Admin"
Assert "admin-dashboard-allowed" ($dash.StatusCode -eq 200) $dash.StatusCode
Assert "admin-dashboard-metrics" ($dash.Content -match "Products \(" -and $dash.Content -match "Orders \(" -and $dash.Content -match "Recorded sales") ""
$prod = Get-Page $adm "/Admin/Products?page=2"
Assert "admin-products-server-paging" ($prod.StatusCode -eq 200 -and $prod.Content -match "page=3") $prod.StatusCode
$detail = Get-Page $adm "/Admin/Product/1"
Assert "admin-product-detail" ($detail.StatusCode -eq 200 -and $detail.Content -match "Classic Cotton Kurta") $detail.StatusCode
$updStock = Post-NoRedir $adm "/Admin/UpdateStock" @{ __RequestVerificationToken = (Get-Token $detail.Content); ProductId = "1"; StockQuantity = "38"; IsActive = "true" }
Assert "admin-update-stock-saves" ($updStock.StatusCode -eq 302) $updStock.StatusCode
$dash2 = Get-Page $adm "/Admin"
Assert "admin-dashboard-link-in-header" ($dash2.Content -match 'href="/Admin"') ""
$plain2 = Register-User "plain2"
$hdr = Get-Page $plain2 "/"
Assert "admin-link-hidden-from-normal-user" ($hdr.Content -notmatch 'href="/Admin"') ""

Write-Output ("FEATURES-RESULT fail=" + $script:fail)
exit $script:fail
