# PX.Generators
Code Generators for Acumatica BQL


How to use:
Create lib folder in root directory
Copy PX.Data.dll and PX.Common.dll to lib folder
Compile with Visual Studio 2022 in Release mode
Open console in `package\PX.Generators.Package\bin\Release`
Execute:
```
nuget.exe add PX.Generators.1.0.0.nupkg -source  "C:\\Program Files (x86)\\Microsoft SDKs\\NuGetPackages\\"
```
Now open Pure.sln or PureWithNetTools.sln (or any other acumatica solution)
Open Manage NuGet Packages for some projected with bql tables
Select Microsoft Visual Studio Offline Packages
Search for PX.Generators
Install it

Now all bql tables for partial IBqlTables or CacheExtensions would be automatically generated (output files are not added to source) if some bql field classes are missing.
