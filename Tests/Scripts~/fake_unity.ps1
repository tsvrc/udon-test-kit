# Stands in for Unity in Test-InvokeUnityTests.ps1, like fake_unity.sh.
if ($env:FAKE_ARGS) { Set-Content -LiteralPath $env:FAKE_ARGS -Value $args }
for ($i = 0; $i -lt $args.Count; $i++) {
    if ($args[$i] -eq '-testResults' -and $env:FAKE_XML) { Copy-Item -LiteralPath $env:FAKE_XML -Destination $args[$i + 1] }
}
exit [int]$env:FAKE_EXIT
