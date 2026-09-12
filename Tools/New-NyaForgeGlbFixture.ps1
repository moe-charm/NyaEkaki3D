[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\Artifacts\NyaForgeGlbFixture\clothing-fixture.glb')
)

$ErrorActionPreference = 'Stop'
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
$parent = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $parent -Force | Out-Null

# A small public fixture for the Windows Authoring import path. It contains a
# static mesh at index 0 and a two-bone skinned mesh at index 1, so the normal
# command-line acceptance path can select mesh 1 without private avatar files.
$bin = [IO.MemoryStream]::new()
$writer = [IO.BinaryWriter]::new($bin)
foreach ($v in @(
    @(0.0, 0.0, 0.0), @(0.1, 0.0, 0.0), @(0.0, 0.1, 0.0)
)) { $writer.Write([single]$v[0]); $writer.Write([single]$v[1]); $writer.Write([single]$v[2]) }
foreach ($index in [uint16[]](0, 1, 2)) { $writer.Write($index) }
while (($bin.Length % 4) -ne 0) { $writer.Write([byte]0) }
$jointOffset = [int]$bin.Position
foreach ($row in @(
    [byte[]](0, 0, 0, 0), [byte[]](0, 1, 0, 0), [byte[]](1, 0, 0, 0)
)) { $writer.Write($row) }
while (($bin.Length % 4) -ne 0) { $writer.Write([byte]0) }
$weightOffset = [int]$bin.Position
foreach ($row in @(
    @(1.0, 0.0, 0.0, 0.0), @(0.5, 0.5, 0.0, 0.0), @(0.0, 1.0, 0.0, 0.0)
)) { foreach ($value in $row) { $writer.Write([single]$value) } }
while (($bin.Length % 4) -ne 0) { $writer.Write([byte]0) }
$inverseOffset = [int]$bin.Position
foreach ($translationY in @(0.0, -0.1)) {
    foreach ($i in 0..15) {
        $value = if ($i -in @(0, 5, 10, 15)) { 1.0 } elseif ($i -eq 13) { $translationY } else { 0.0 }
        $writer.Write([single]$value)
    }
}
$writer.Flush()
$binBytes = $bin.ToArray()
$writer.Dispose(); $bin.Dispose()

$view = @(
    [ordered]@{ buffer = 0; byteOffset = 0; byteLength = 36 },
    [ordered]@{ buffer = 0; byteOffset = 36; byteLength = 6 },
    [ordered]@{ buffer = 0; byteOffset = $jointOffset; byteLength = 12 },
    [ordered]@{ buffer = 0; byteOffset = $weightOffset; byteLength = 48 },
    [ordered]@{ buffer = 0; byteOffset = $inverseOffset; byteLength = 128 }
)
$accessor = @(
    [ordered]@{ bufferView = 0; componentType = 5126; count = 3; type = 'VEC3' },
    [ordered]@{ bufferView = 1; componentType = 5123; count = 3; type = 'SCALAR' },
    [ordered]@{ bufferView = 2; componentType = 5121; count = 3; type = 'VEC4' },
    [ordered]@{ bufferView = 3; componentType = 5126; count = 3; type = 'VEC4' },
    [ordered]@{ bufferView = 4; componentType = 5126; count = 2; type = 'MAT4' }
)
$material = [ordered]@{
    name = 'Fixture red';
    pbrMetallicRoughness = [ordered]@{ baseColorFactor = @(0.8, 0.1, 0.12, 1.0); metallicFactor = 0.05; roughnessFactor = 0.7 }
}
$staticPrimitive = [ordered]@{ attributes = [ordered]@{ POSITION = 0 }; indices = 1; material = 0 }
$skinnedPrimitive = [ordered]@{ attributes = [ordered]@{ POSITION = 0; JOINTS_0 = 2; WEIGHTS_0 = 3 }; indices = 1; material = 0 }
$json = [ordered]@{
    asset = [ordered]@{ version = '2.0'; generator = 'NyaForge public fixture' }
    scene = 0
    scenes = @([ordered]@{ nodes = @(0, 2) })
    buffers = @([ordered]@{ byteLength = $binBytes.Length })
    bufferViews = $view
    accessors = $accessor
    materials = @($material)
    meshes = @(
        [ordered]@{ name = 'Fixture static'; primitives = @($staticPrimitive) },
        [ordered]@{ name = 'Fixture clothing'; primitives = @($skinnedPrimitive) }
    )
    nodes = @(
        [ordered]@{ name = 'Root'; children = @(1) },
        [ordered]@{ name = 'Child'; mesh = 1; skin = 0; translation = @(0.0, 0.1, 0.0) },
        [ordered]@{ name = 'Backdrop'; mesh = 0 }
    )
    skins = @([ordered]@{ joints = @(0, 1); inverseBindMatrices = 4 })
}
$jsonBytes = [Text.UTF8Encoding]::new($false).GetBytes(($json | ConvertTo-Json -Compress -Depth 20))
while (($jsonBytes.Length % 4) -ne 0) { $jsonBytes += [byte]0x20 }
$jsonBytes = [byte[]]$jsonBytes

$totalLength = 12 + 8 + $jsonBytes.Length + 8 + $binBytes.Length
$output = [IO.FileStream]::new($OutputPath, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::None)
$glb = [IO.BinaryWriter]::new($output)
$glb.Write([uint32]0x46546C67); $glb.Write([uint32]2); $glb.Write([uint32]$totalLength)
$glb.Write([uint32]$jsonBytes.Length); $glb.Write([uint32]0x4E4F534A); $glb.Write($jsonBytes, 0, $jsonBytes.Length)
$glb.Write([uint32]$binBytes.Length); $glb.Write([uint32]0x004E4942); $glb.Write($binBytes, 0, $binBytes.Length)
$glb.Flush(); $glb.Dispose(); $output.Dispose()
if ((Get-Item -LiteralPath $OutputPath).Length -ne $totalLength) {
    throw "GLB fixture length mismatch: expected $totalLength bytes, got $((Get-Item -LiteralPath $OutputPath).Length)"
}
Write-Output "Created GLB fixture: $OutputPath ($totalLength bytes)"
