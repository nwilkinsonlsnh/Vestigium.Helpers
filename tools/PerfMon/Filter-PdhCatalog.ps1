#Requires -Version 5.1
<#
.SYNOPSIS
  Filter a Dump-PdhCatalog JSON to one probe allow-list.

.EXAMPLE
  .\Filter-PdhCatalog.ps1 -Dump .\dumps\pdh-all.json -Probe PageFile

.EXAMPLE
  .\Filter-PdhCatalog.ps1 -Dump .\dumps\pdh-all.json -AllowList @('Paging File') -OutFile .\shards\pagefile.json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $Dump,
    [string[]] $AllowList,
    [string] $OutFile,
    [ValidateSet('PageFile','Memory','Cpu','Disk','Gpu','Network')]
    [string] $Probe
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

$probeAllow = @{
    PageFile = @('Paging File')
    Memory   = @('Cache', 'Hyper-V Dynamic Memory Integration Service', 'Memory', 'NUMA Node Memory', 'ReadyBoost Cache')
    Cpu      = @('Processor', 'Processor Information', 'Processor Performance')
    Disk     = @(
        'FileSystem Disk Activity', 'LogicalDisk', 'Ntfs Bucketized Performance', 'PhysicalDisk',
        'ReFS', 'ReFS Bucketized Performance', 'ReFS Dedup Minstore Perf Counters', 'ReFS Dedup Perf Counters',
        'Storage Management WSP Spaces Runtime', 'Storage Spaces Drt', 'Storage Spaces Tier',
        'Storage Spaces Virtual Disk', 'Storage Spaces Virtual Disk Io', 'Storage Spaces Virtual Disk Map',
        'Storport Unit Queue', 'Storport Unit Reads', 'Storport Unit Transfers', 'Storport Unit Writes',
        'VHD Bucketized Performance'
    )
    Gpu      = @('GPU Engine', 'GPU Process Memory', 'GPU Adapter Memory', 'GPU Local Adapter Memory', 'GPU Non Local Adapter Memory')
    Network  = @(
        'Per Processor Network Activity Cycles', 'Per Processor Network Interface Card Activity',
        'TCPIP Performance Diagnostics (Per-CPU)', 'Bluetooth Device', 'Bluetooth Radio', 'DNS64 Global',
        'Firewall Rules by Profile', 'Firewall Rules by Store', 'HTTP Service', 'HTTP Service Request Queues',
        'HTTP Service Url Groups', 'Hyper-V Virtual Machine Bus Pipes', 'ICMP', 'ICMPv6', 'IPHTTPS Global',
        'IPHTTPS Session', 'IPsec AuthIP IPv4', 'IPsec AuthIP IPv6', 'IPsec Connections', 'IPsec Driver',
        'IPsec IKEv1 IPv4', 'IPsec IKEv1 IPv6', 'IPsec IKEv2 IPv4', 'IPsec IKEv2 IPv6', 'IPv4', 'IPv6',
        'Network Adapter', 'Network Interface', 'Network QoS Policy', 'PacketDirect EC Utilization',
        'PacketDirect Queue Depth', 'PacketDirect Receive Counters', 'PacketDirect Receive Filters',
        'PacketDirect Transmit Counters', 'Physical Network Interface Card Activity', 'RemoteFX Network',
        'SMB Client Shares', 'SMB Direct', 'SMB Server', 'SMB Server Sessions', 'SMB Server Shares',
        'TCPIP Extended Performance Diagnostics', 'TCPIP Performance Diagnostics',
        'TCPIP Transport Layer Packet Drop Counters', 'TCPv4', 'TCPv6', 'Teredo Client', 'Teredo Relay',
        'Teredo Server', 'UDPv4', 'UDPv6', 'WFP', 'WFP Classify', 'WFP Filter Count', 'WFP Filter Size',
        'WFP Reauthorization', 'WFPv4', 'WFPv6', 'WinNAT', 'WinNAT ICMP', 'WinNAT Instance', 'WinNAT TCP',
        'WinNAT UDP'
    )
}

if ($Probe) {
    $AllowList = $probeAllow[$Probe]
    if (-not $OutFile) {
        $OutFile = Join-Path $repoRoot "src\Vestigium.Helpers.PerfMon.$Probe\EventCatalog\pdh-categories.json"
    }
}

if (-not $AllowList -or $AllowList.Count -eq 0) {
    throw "Pass -Probe PageFile (or Memory/Cpu/Disk/Gpu/Network) or an explicit -AllowList."
}
if (-not $OutFile) {
    throw "Pass -OutFile or -Probe so the shard lands under src\\Vestigium.Helpers.PerfMon.<Probe>\\EventCatalog."
}

function Resolve-DumpPath {
    param([string] $Dump)
    if ([System.IO.Path]::IsPathRooted($Dump) -and (Test-Path $Dump)) { return $Dump }
    $candidates = @(
        (Join-Path (Get-Location) $Dump),
        (Join-Path $PSScriptRoot $Dump),
        (Join-Path $PSScriptRoot (Join-Path 'dumps' $Dump)),
        (Join-Path $PSScriptRoot 'dumps\pdh-all.json'),
        (Join-Path $PSScriptRoot 'pdh-all.json'),
        (Join-Path $repoRoot $Dump),
        (Join-Path $repoRoot (Join-Path 'tools\PerfMon\dumps' $Dump))
    )
    foreach ($path in $candidates) {
        if ($path -and (Test-Path $path)) { return (Resolve-Path $path).Path }
    }
    throw "Dump not found: $Dump. Pass the full path to pdh-all.json (it is not in git)."
}

$dumpPath = Resolve-DumpPath $Dump

$outPath = $OutFile
if (-not [System.IO.Path]::IsPathRooted($outPath)) {
    $outPath = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $outPath))
}
if ($outPath -match '^[A-Za-z]:\\src\\') {
    throw "Refusing $outPath. Use -Probe PageFile so the file goes under $repoRoot\src\..."
}

$raw = Get-Content -Path $dumpPath -Raw -Encoding UTF8
$data = $raw | ConvertFrom-Json
$wanted = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($name in $AllowList) {
    $trim = $name.Trim()
    if ($trim) { [void]$wanted.Add($trim) }
}

$rows = New-Object System.Collections.Generic.List[object]
foreach ($cat in @($data.categories)) {
    if (-not $wanted.Contains([string]$cat.category)) { continue }
    $counters = @()
    foreach ($c in @($cat.counters)) {
        $counters += [pscustomobject]@{
            name       = [string]$c.name
            identifier = [string]$c.identifier
        }
    }
    $rows.Add([pscustomobject]@{
        category   = [string]$cat.category
        identifier = [string]$cat.identifier
        type       = [string]$cat.type
        counters   = $counters
    })
}

$payload = [pscustomobject]@{
    source        = [System.IO.Path]::GetFileName($dumpPath)
    machine       = [string]$data.machine
    utc           = [string]$data.utc
    allowList     = @($AllowList)
    categoryCount = $rows.Count
    categories    = $rows
}

$dir = Split-Path -Parent $outPath
if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
function ConvertTo-StableJson {
    param($Value, [int] $Level = 0)
    $pad = "  " * $Level
    $inner = "  " * ($Level + 1)

    if ($null -eq $Value) { return "null" }

    if ($Value -is [bool]) {
        if ($Value) { return "true" }
        return "false"
    }

    if ($Value -is [int] -or $Value -is [long] -or $Value -is [decimal] -or $Value -is [double]) {
        return ([string]$Value)
    }

    if ($Value -is [string]) {
        $escaped = $Value.Replace('\', '\\').Replace('"', '\"')
        return ('"' + $escaped + '"')
    }

    $note = @($Value.PSObject.Properties | Where-Object { $_.MemberType -eq "NoteProperty" })
    if ($note.Count -gt 0) {
        $preferred = @("source","machine","utc","allowList","categoryCount","categories","category","identifier","type","counters","name")
        $names = New-Object System.Collections.Generic.List[string]
        foreach ($n in $preferred) {
            foreach ($pr in $note) {
                if ($pr.Name -eq $n) { [void]$names.Add($n) }
            }
        }
        foreach ($pr in $note) {
            if (-not $names.Contains($pr.Name)) { [void]$names.Add($pr.Name) }
        }
        $parts = New-Object System.Collections.Generic.List[string]
        foreach ($n in $names) {
            $rendered = ConvertTo-StableJson -Value $Value.$n -Level ($Level + 1)
            [void]$parts.Add(($inner + '"' + $n + '": ' + $rendered))
        }
        if ($parts.Count -eq 0) { return "{}" }
        return ("{`n" + ($parts -join ",`n") + "`n" + $pad + "}")
    }

    $items = @($Value)
    if ($items.Count -eq 0) { return "[]" }
    $parts = New-Object System.Collections.Generic.List[string]
    foreach ($item in $items) {
        [void]$parts.Add(($inner + (ConvertTo-StableJson -Value $item -Level ($Level + 1))))
    }
    return ("[`n" + ($parts -join ",`n") + "`n" + $pad + "]")
}

$json = ConvertTo-StableJson -Value $payload
if (-not $json.EndsWith("`n")) { $json = $json + "`n" }
$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($outPath, $json, $utf8)
Write-Output $outPath
