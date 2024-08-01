$PUBLISH_TARGET = "..\out\ClassIsland.PluginIndexGenerator"

if ($(Test-Path ./out) -eq $false) {
    mkdir out
} else {
    rm out/* -Recurse -Force
}


    
dotnet publish .\ClassIsland.PluginIndexGenerator\ClassIsland.PluginIndexGenerator.csproj -c Release -p:PublishProfile=FolderProfile -p:PublishDir=$PUBLISH_TARGET -property:DebugType=embedded

Write-Host "Successfully published to $PUBLISH_TARGET" -ForegroundColor Green

Write-Host "Packaging..." -ForegroundColor Cyan

rm ./out/ClassIsland.PluginIndexGenerator/*.xml

7z a "./out/ClassIsland.PluginIndexGenerator.zip" ./out/ClassIsland.PluginIndexGenerator/* -r -mx=9

rm -Recurse -Force ./out/ClassIsland.PluginIndexGenerator
