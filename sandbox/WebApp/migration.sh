#!/bin/bash
dotnet tool restore
dotnet ef database update docker
