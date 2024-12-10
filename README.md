FEZ
===

## Description:
* Complete Source Code of indie game FEZ.

## Compilation:
1. Open Visual Studio (2019+)
2. Build FezEngine  
  a. File -> Open -> Project/Solution  
  b. Open FezEngine/FezEngine.sln  
  c. Build -> Build Solution  
3. Build FEZ  
  a. Open FEZ/FEZ.sln  
  b. Add FezEngine to the solution
    - In the Solution Explorer, open Dependancies and right-click Assemblies  
    - Click "Add Assembly Reference"  
    - Click "Browse..." and select the FezEngine DLL at FezEngine/bin/Debug/net40   

    c. After building the solution, the executable will now be in FEZ/bin/Debug/net40.  
4. Copy all files aside from FEZ.exe and FezEngine.dll from your base game files into this directory.  
5. The game should run flawlessly.
