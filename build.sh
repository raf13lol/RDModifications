#!/bin/bash

echo "Building BPE5 build..."
dotnet build /p:BPE5=1
cp bin/Debug/netstandard2.1/com.rhythmdr.randommodifications.dll bin/Debug/netstandard2.1/com.rhythmdr.bpe5randommodifications.dll
echo "Building BPE6 build..."
dotnet build