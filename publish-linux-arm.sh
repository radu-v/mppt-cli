#!/bin/sh

dotnet publish -r linux-arm mppt-cli.sln  -c release /p:TrimUnusedDependencies=true
