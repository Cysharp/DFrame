dotnet tool restore
dotnet mpc -i .\Assets\Plugins\DFrame\Runtime\Unity -o .\Assets\Plugins\DFrame\Runtime\Unity\MessagePackGenerated.cs -n DFrame

dotnet build .\DFrame.HubDefinition.csproj
dotnet moc -i .\DFrame.HubDefinition.csproj -o .\Assets\Plugins\DFrame\Runtime\Unity\MagicOnionGenerated.cs -n DFrame -m DFrame