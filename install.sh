#!/bin/bash

cd r0b0t
dotnet publish -c Release
dotnet pack
dotnet tool uninstall -g r0b0t
dotnet tool install -g r0b0t --add-source nupkg
