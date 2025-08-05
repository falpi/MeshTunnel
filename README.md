# MeshTunnel 🚇  

[![.NET 8](https://img.shields.io/badge/.NET-8-%23512bd4)](https://dotnet.microsoft.com) 
[![License](https://img.shields.io/badge/license-Apache%202.0-blue)](LICENSE)  

*A lightweight, cross-platform console version of MeshCentral Router, the tool that performs TCP/UDP port mapping thru the MeshCentral server.*


## 🔍 Overview  
**MeshTunnel** is a streamlined, console-based port forwarding tool derived from [MeshCentral Router](https://github.com/Ylianst/MeshCentralRouter). While the original project is Windows-only and GUI-driven, MeshTunnel:  
✔ **Runs anywhere** – Pure .NET 8 rewrite for full Linux/macOS/Windows support  
✔ **Keeps compatibility** – Works with existing MeshCentral servers and `.mcrouter` rule files  
✔ **Simplifies automation** – No GUI overhead, ideal for scripting and headless environments  


## 🚀 Key Differences  
| Feature               | Original               | MeshTunnel            |
|-----------------------|------------------------|------------------------|
| Platform              | Windows-only           | **Cross-platform**     |
| Interface             | GUI                    | **Console**            |
| .mcrouter support     | ✅                     | ✅                     |
| Dependencies          | WinForms               | **Pure .NET 8**        |


## 🛠️ Usage

```bash
MeshTunnel myrules.mcrouter
```
