param(
    [Parameter(Mandatory = $true)][int]$BuildNumber
)
$ErrorActionPreference = 'Stop'
if ($BuildNumber -lt 100) { throw "Compilación fuera de rango: $BuildNumber" }
if ($BuildNumber -lt 1000) {
    $major = 6
    $minor = 3 + [int][Math]::Floor($BuildNumber / 100)
}
else {
    $major = 7 + [int][Math]::Floor(($BuildNumber - 1000) / 1000)
    $minor = [int][Math]::Floor((($BuildNumber - 1000) % 1000) / 100)
}
$revision = $BuildNumber % 100
$version = '{0}.{1}.{2:D2}' -f $major, $minor, $revision
$tag = "v$version"
Write-Host "JANUS Dock Build #$BuildNumber => $version ($tag)"
if ($env:GITHUB_ENV) {
    "JANUS_VERSION=$version" | Out-File -FilePath $env:GITHUB_ENV -Append -Encoding utf8
    "JANUS_RELEASE_TAG=$tag" | Out-File -FilePath $env:GITHUB_ENV -Append -Encoding utf8
}
if ($env:GITHUB_OUTPUT) {
    "version=$version" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    "tag=$tag" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
}
