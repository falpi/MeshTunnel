# MeshTunnel 🚇  

[![.NET 8](https://img.shields.io/badge/.NET-8-%23512bd4)](https://dotnet.microsoft.com) 
[![License](https://img.shields.io/badge/license-Apache%202.0-blue)](LICENSE)  

*A lightweight, cross-platform console version of MeshCentral Router, the tool that performs TCP/UDP port mapping thru the MeshCentral server.*


## 🔍 Overview  
**MeshTunnel** is a streamlined, console-based port forwarding tool derived from [MeshCentral Router](https://github.com/Ylianst/MeshCentralRouter).</br>
While the original project is Windows-only and GUI-driven, MeshTunnel:  
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

## ⚙️ Build
There are already three different publish profiles for Windows, MacOS, and Linux for building the project. These can be invoked from the IDE or from the command line as follows:
```bash
dotnet publish -p:PublishProfile=publish-win-x64
dotnet publish -p:PublishProfile=publish-osx-x64
dotnet publish -p:PublishProfile=publish-linux-x64
```

## 🛠️ Usage

```bash
MeshTunnel myrules.mcrouter
```

## 📁 Mapping Rules File Format

The `sample.mcrouter` file illustrates the minimum subset of attributes needed to configure one or more mapping rules, according to the format established by the original "MeshCentral Router" tool. You can build these files manually or create the rules using the MeshCentral Router GUI for Windows and then export them in JSON format.
The following is an example of the configuration file.

```json
{
  "hostname": "mesh.example.com/?key=optional_login_key",
  "username": "admin",
  "password": "manager",
  "mappings": [{
      "nodeName": "DBSERVER",
      "name": "MySQL Rule",
      "nodeId": "node//9876543210",
      "protocol": 0,
      "localPort": 3306,
      "remotePort": 3306
    }
  ]
}
```

## 🙌 Credits

- **Original Project**: [MeshCentral Router](https://github.com/Ylianst/MeshCentralRouter) by [Ylianst](https://github.com/Ylianst)

*Not affiliated with the MeshCentral project.*
