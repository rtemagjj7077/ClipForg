#!/bin/bash
set -e
echo "Publishing self-contained ClipForge for Linux x64..."
dotnet publish ClipForge.Platform.Linux/ClipForge.Platform.Linux.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o publish/linux-x64
echo "Published binary: publish/linux-x64/ClipForge"
