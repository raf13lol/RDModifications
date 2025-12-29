#!/bin/bash

dotnet build /p:BPE5=1
cp bin/Debug/netstandard2.1/com.rhythmdr.randommodifications.dll bin/Debug/netstandard2.1/com.rhythmdr.bpe5randommodifications.dll
dotnet build