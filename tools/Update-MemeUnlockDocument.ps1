[CmdletBinding()]
param(
    [string]$RimWorldPath = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld',
    [string]$WorkshopPath = 'C:\Program Files (x86)\Steam\steamapps\workshop\content\294100',
    [string]$ModsConfigPath = "$env:USERPROFILE\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\ModsConfig.xml",
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputPath) {
    $OutputPath = Join-Path $repoRoot 'docs\STEAM_DISCUSSION_MEME_UNLOCKS.txt'
}

function Get-XmlFile {
    param([Parameter(Mandatory)][string]$Path)

    try {
        return [xml](Get-Content -LiteralPath $Path -Raw)
    }
    catch {
        Write-Warning "Skipping invalid XML: $Path ($($_.Exception.Message))"
        return $null
    }
}

function Get-NodeText {
    param(
        [Parameter(Mandatory)]$Node,
        [Parameter(Mandatory)][string]$XPath
    )

    $match = $Node.SelectSingleNode($XPath)
    if ($null -eq $match) {
        return ''
    }

    return $match.InnerText.Trim()
}

function Get-InheritedMemeField {
    param(
        [Parameter(Mandatory)]$Node,
        [Parameter(Mandatory)][string]$Field,
        [Parameter(Mandatory)][hashtable]$Templates
    )

    $current = $Node
    $seen = @{}
    while ($null -ne $current) {
        $value = Get-NodeText -Node $current -XPath $Field
        if ($value) {
            return $value
        }

        $parentName = $current.GetAttribute('ParentName')
        if (-not $parentName -or $seen.ContainsKey($parentName) -or -not $Templates.ContainsKey($parentName)) {
            break
        }

        $seen[$parentName] = $true
        $current = $Templates[$parentName]
    }

    return ''
}

function ConvertTo-PlayerLabel {
    param([Parameter(Mandatory)][string]$Value)

    if ($Value.Length -eq 0) {
        return $Value
    }

    return [char]::ToUpperInvariant($Value[0]) + $Value.Substring(1)
}

if (-not (Test-Path -LiteralPath $ModsConfigPath)) {
    throw "RimWorld mod configuration was not found: $ModsConfigPath"
}

$config = Get-XmlFile -Path $ModsConfigPath
$gameVersion = Get-NodeText -Node $config -XPath '/ModsConfigData/version'
$activePackages = @(
    $config.SelectNodes('/ModsConfigData/activeMods/li') |
        ForEach-Object { $_.InnerText.Trim().ToLowerInvariant() }
)
$activeSet = @{}
foreach ($packageId in $activePackages) {
    $activeSet[$packageId] = $true
}

$catalog = @{}
$catalogRoots = @(
    (Join-Path $RimWorldPath 'Data'),
    (Join-Path $RimWorldPath 'Mods'),
    $WorkshopPath
)
foreach ($catalogRoot in $catalogRoots) {
    if (-not (Test-Path -LiteralPath $catalogRoot)) {
        continue
    }

    foreach ($directory in Get-ChildItem -LiteralPath $catalogRoot -Directory) {
        $aboutPath = Join-Path $directory.FullName 'About\About.xml'
        if (-not (Test-Path -LiteralPath $aboutPath)) {
            continue
        }

        $about = Get-XmlFile -Path $aboutPath
        if ($null -eq $about) {
            continue
        }

        $packageId = (Get-NodeText -Node $about -XPath '/ModMetaData/packageId').ToLowerInvariant()
        if (-not $packageId) {
            continue
        }

        $name = Get-NodeText -Node $about -XPath '/ModMetaData/name'
        if (-not $name) {
            $name = $directory.Name
        }

        $catalog[$packageId] = [pscustomobject]@{
            Name      = $name
            PackageId = $packageId
            Path      = $directory.FullName
        }
    }
}

function Get-ActiveLoadFolders {
    param(
        [Parameter(Mandatory)]$Mod,
        [Parameter(Mandatory)][hashtable]$EnabledPackages
    )

    $loadFoldersPath = Join-Path $Mod.Path 'LoadFolders.xml'
    if (Test-Path -LiteralPath $loadFoldersPath) {
        $loadFoldersXml = Get-XmlFile -Path $loadFoldersPath
        if ($null -ne $loadFoldersXml) {
            $folders = @()
            foreach ($entry in @($loadFoldersXml.SelectNodes('/loadFolders/v1.6/li'))) {
                $include = $true
                $ifActive = $entry.GetAttribute('IfModActive')
                $ifInactive = $entry.GetAttribute('IfModNotActive')

                if ($ifActive) {
                    $include = $EnabledPackages.ContainsKey($ifActive.ToLowerInvariant())
                }
                if ($ifInactive -and $EnabledPackages.ContainsKey($ifInactive.ToLowerInvariant())) {
                    $include = $false
                }

                if ($include) {
                    $relativePath = $entry.InnerText.Trim()
                    if ($relativePath -eq '/') {
                        $relativePath = ''
                    }
                    $folders += Join-Path $Mod.Path $relativePath
                }
            }

            return @($folders | Select-Object -Unique)
        }
    }

    $versionPath = Join-Path $Mod.Path '1.6'
    if (Test-Path -LiteralPath $versionPath) {
        return @($versionPath)
    }

    return @($Mod.Path)
}

$researchLabels = @{
    AdvancedPsychicRituals = 'Advanced psychic rituals'
    BasicPsychicRituals    = 'Basic psychic rituals'
    BRM_Emergence_Medieval = 'Medieval Emergence'
    Biosculpting           = 'Biosculpting'
    Brewing                = 'Beer brewing'
    ComplexFurniture       = 'Complex furniture'
    Deathrest              = 'Deathrest'
    Electricity            = 'Electricity'
    Fabrication            = 'Fabrication'
    FertilityProcedures    = 'Fertility procedures'
    Gunsmithing            = 'Gunsmithing'
    GR_GeneticEngineering  = 'Genetic creation'
    HospitalBed            = 'Hospital beds'
    Hydroponics            = 'Hydroponics'
    LongBlades             = 'Long blades'
    Machining              = 'Machining'
    MedicineProduction     = 'Medicine production'
    OrbitalTech            = 'Orbital technology'
    Prosthetics            = 'Prosthetics'
    PsychoidBrewing        = 'Psychoid brewing'
    Smithing               = 'Smithing'
    Stonecutting           = 'Stonecutting'
    TreeSowing             = 'Tree sowing'
    VVE_BasicVehicles      = 'Basic vehicles'
}

$conditionalRequirementLabels = @{
    'ferny.noderesearch' = 'Node Research'
}

$unlockConditions = @{}
$availabilityClass = 'IdeologyReformation.Restrictions.MemeAvailabilityExtension'
$availabilityFiles = @(
    Get-ChildItem -LiteralPath (Join-Path $repoRoot '1.6\Patches') -Recurse -File -Filter '*.xml' -ErrorAction SilentlyContinue
    Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Mods') -Recurse -File -Filter '*.xml' -ErrorAction SilentlyContinue
) | Where-Object {
    Select-String -LiteralPath $_.FullName -SimpleMatch $availabilityClass -Quiet
}

foreach ($availabilityFile in $availabilityFiles) {
    $patch = Get-XmlFile -Path $availabilityFile.FullName
    if ($null -eq $patch) {
        continue
    }

    foreach ($operation in @($patch.SelectNodes("//*[xpath and value/li[@Class='$availabilityClass']]"))) {
        $extension = $operation.SelectSingleNode("value/li[@Class='$availabilityClass']")
        if ($null -eq $extension) {
            continue
        }

        $xpath = Get-NodeText -Node $operation -XPath 'xpath'
        $minimumTech = Get-NodeText -Node $extension -XPath 'minTechLevel'
        $research = @(
            $extension.SelectNodes('requiredResearch/li') |
                ForEach-Object { $_.InnerText.Trim() } |
                Where-Object { $_ }
        )
        $research += @(
            $extension.SelectNodes('requiredResearchDefNames/li') |
                ForEach-Object { $_.InnerText.Trim() } |
                Where-Object { $_ }
        )

        if ($minimumTech) {
            $condition = "Tech level: $minimumTech"
        }
        elseif ($research.Count -gt 0) {
            $researchNames = foreach ($researchDefName in $research) {
                if ($researchLabels.ContainsKey($researchDefName)) {
                    $researchLabels[$researchDefName]
                }
                else {
                    $researchDefName
                }
            }
            $condition = 'Research: ' + ($researchNames -join ' + ')
        }
        else {
            $condition = 'Unrestricted'
        }

        $mayRequire = $operation.GetAttribute('MayRequire').ToLowerInvariant()
        if ($mayRequire -and $conditionalRequirementLabels.ContainsKey($mayRequire)) {
            $condition += " (when $($conditionalRequirementLabels[$mayRequire]) is enabled)"
        }

        $findModOperation = $operation.SelectSingleNode('ancestor::Operation[@Class="PatchOperationFindMod"][1]')
        if ($null -ne $findModOperation) {
            $conditionalMods = @(
                $findModOperation.SelectNodes('mods/li') |
                    ForEach-Object { $_.InnerText.Trim() } |
                    Where-Object { $_ }
            )
            if ($conditionalMods.Count -gt 0) {
                $modList = $conditionalMods -join ' or '
                if ($operation.Name -eq 'nomatch') {
                    $condition += " (when $modList is not enabled)"
                }
                elseif ($operation.Name -eq 'match') {
                    $condition += " (when $modList is enabled)"
                }
            }
        }

        foreach ($match in [regex]::Matches($xpath, 'defName\s*=\s*["'']([^"'']+)["'']')) {
            $unlockConditions[$match.Groups[1].Value] = $condition
        }
    }
}

$memes = @{}
for ($loadIndex = 0; $loadIndex -lt $activePackages.Count; $loadIndex++) {
    $packageId = $activePackages[$loadIndex]
    if (-not $catalog.ContainsKey($packageId)) {
        continue
    }

    $mod = $catalog[$packageId]
    $nodes = @()
    $templates = @{}

    foreach ($loadFolder in Get-ActiveLoadFolders -Mod $mod -EnabledPackages $activeSet) {
        $defsPath = Join-Path $loadFolder 'Defs'
        if (-not (Test-Path -LiteralPath $defsPath)) {
            continue
        }

        foreach ($definitionFile in Get-ChildItem -LiteralPath $defsPath -Recurse -File -Filter '*.xml') {
            $definitions = Get-XmlFile -Path $definitionFile.FullName
            if ($null -eq $definitions) {
                continue
            }

            foreach ($node in @($definitions.SelectNodes('/Defs/MemeDef'))) {
                $nodes += $node
                $templateName = $node.GetAttribute('Name')
                if ($templateName) {
                    $templates[$templateName] = $node
                }
            }
        }
    }

    foreach ($node in $nodes) {
        $defName = Get-NodeText -Node $node -XPath 'defName'
        if (-not $defName) {
            continue
        }

        $label = Get-InheritedMemeField -Node $node -Field 'label' -Templates $templates
        if (-not $label) {
            $label = $defName
        }

        $category = Get-InheritedMemeField -Node $node -Field 'category' -Templates $templates
        if (-not $category) {
            $category = 'Normal'
        }

        $factionWhitelist = Get-InheritedMemeField -Node $node -Field 'factionWhitelist' -Templates $templates
        $condition = if ($unlockConditions.ContainsKey($defName)) {
            $unlockConditions[$defName]
        }
        else {
            'Unrestricted'
        }

        # Later definitions win, matching RimWorld's package load order behavior.
        $memes[$defName] = [pscustomobject]@{
            Provider         = $mod.Name
            PackageId        = $packageId
            LoadIndex        = $loadIndex
            Label            = $label
            DefName          = $defName
            Category         = $category
            UnlockCondition  = $condition
            FactionWhitelist = $factionWhitelist
        }
    }
}

$rows = @($memes.Values)
$restrictedCount = @($rows | Where-Object { $_.UnlockCondition -ne 'Unrestricted' }).Count
$unrestrictedCount = $rows.Count - $restrictedCount
$providerCount = @($rows.Provider | Select-Object -Unique).Count
$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('[h1]Ideology Reformation — Meme Unlock Guide[/h1]')
$lines.Add('')
$lines.Add('Ideology Reformation makes selected memes become available as your colony advances. This reference lists every supported meme and the requirement, if any, added by the mod.')
$lines.Add('')
$lines.Add('[list]')
$lines.Add('[*][b]Research requirement[/b] — complete the named research project.')
$lines.Add('[*][b]Tech-level requirement[/b] — advance the colony to at least that technology level.')
$lines.Add('[*][b]Available immediately[/b] — Ideology Reformation adds no progression requirement. The relevant DLC or content mod must still be enabled.')
$lines.Add('[/list]')
$lines.Add('')
$lines.Add("[i]Current coverage: $($rows.Count) memes and structures from $providerCount supported sources; $restrictedCount have progression requirements and $unrestrictedCount are available immediately.[/i]")
$lines.Add('')

$providerGroups = $rows |
    Group-Object Provider |
    Sort-Object { ($_.Group | Measure-Object -Property LoadIndex -Minimum).Minimum }
foreach ($providerGroup in $providerGroups) {
    $providerName = if ($providerGroup.Name -eq 'Ideology') { 'Core Ideology' } else { $providerGroup.Name }
    $lines.Add("[h2]$providerName[/h2]")
    $lines.Add('')

    $gated = @($providerGroup.Group | Where-Object { $_.UnlockCondition -ne 'Unrestricted' -and $_.Category -ne 'Structure' } | Sort-Object Label)
    $immediateMemes = @($providerGroup.Group | Where-Object { $_.UnlockCondition -eq 'Unrestricted' -and $_.Category -ne 'Structure' } | Sort-Object Label)
    $immediateStructures = @($providerGroup.Group | Where-Object { $_.UnlockCondition -eq 'Unrestricted' -and $_.Category -eq 'Structure' } | Sort-Object Label)
    $gatedStructures = @($providerGroup.Group | Where-Object { $_.UnlockCondition -ne 'Unrestricted' -and $_.Category -eq 'Structure' } | Sort-Object Label)

    if ($gated.Count -gt 0) {
        $lines.Add('[b]Progression unlocks[/b]')
        $lines.Add('[list]')
        foreach ($meme in $gated) {
            $label = ConvertTo-PlayerLabel $meme.Label
            $lines.Add("[*][b]$label[/b] — $($meme.UnlockCondition)")
        }
        $lines.Add('[/list]')
        $lines.Add('')
    }

    if ($immediateMemes.Count -gt 0) {
        $lines.Add('[b]Available immediately[/b]')
        $lines.Add('[list]')
        foreach ($meme in $immediateMemes) {
            $label = ConvertTo-PlayerLabel $meme.Label
            $lines.Add("[*]$label")
        }
        $lines.Add('[/list]')
        $lines.Add('')
    }

    if ($immediateStructures.Count -gt 0) {
        $heading = if ($gatedStructures.Count -eq 0) { '[b]Structures — available immediately[/b]' } else { '[b]Structures available immediately[/b]' }
        $lines.Add($heading)
        $lines.Add('[list]')
        foreach ($meme in $immediateStructures) {
            $label = ConvertTo-PlayerLabel $meme.Label
            $lines.Add("[*]$label")
        }
        $lines.Add('[/list]')
        $lines.Add('')
    }

    if ($gatedStructures.Count -gt 0) {
        $lines.Add('[b]Structure progression unlocks[/b]')
        $lines.Add('[list]')
        foreach ($meme in $gatedStructures) {
            $label = ConvertTo-PlayerLabel $meme.Label
            $lines.Add("[*][b]$label[/b] — $($meme.UnlockCondition)")
        }
        $lines.Add('[/list]')
    }

    $lines.Add('')
}

$lines.Add('[h2]Notes[/h2]')
$lines.Add('')
$lines.Add('[list]')
$lines.Add('[*]Structure choices are listed for completeness. They currently have no progression requirements.')
$lines.Add('[*]Inhuman and Shipborn also retain the faction restrictions supplied by their original DLC definitions.')
$lines.Add('[*]Requirements may change as Ideology Reformation and supported content mods are updated.')
$lines.Add('[/list]')

$outputDirectory = Split-Path -Parent $OutputPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

[System.IO.File]::WriteAllLines($OutputPath, $lines, [System.Text.UTF8Encoding]::new($false))
Write-Output "Wrote Steam-ready player reference with $($rows.Count) memes from $providerCount providers to $OutputPath"
