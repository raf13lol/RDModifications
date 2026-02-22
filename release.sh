#!/bin/bash

gh release create $1 -F CHANGELOG.txt
gh release upload $1 com.rhythmdr.bpe5randommodifications.dll com.rhythmdr.randommodifications.dll