# FolderMount

> **Map local folders to virtual drive letters using Windows SUBST — no admin required.**

FolderMount is a polished Windows system tray app that lets you assign drive letters (e.g. `P:`, `M:`) to any folder on your machine. All mappings persist across reboots and are re-applied silently at login.

---

## Features

| Feature | Details |
|---|---|
| 🗂 Virtual drives | Map any folder → drive letter via `SUBST` |
| 🔄 Auto-mount on login | Silent startup via Windows Registry (no admin needed) |
| 📋 XML persistence | Mappings saved to `%APPDATA%\FolderMount\mappings.xml` |
| 📤 Export / Import | Back up or share your mapping file as XML |
| 🟢 Live status | Green/gray status dot per drive |
| ⚡ Mount All / Disconnect All | Bulk operations in one click |
| 📂 Open in Explorer | Jump straight to the virtual drive |
| 🏷️ Labels | Give each drive a friendly name |
| ⚙️ Settings | Toggle startup, open data folder, quick export |
| 🔵 System tray | Lives quietly in the tray, minimize-to-tray on close |
| 🎨 Dark theme | Follows your Windows theme preference |

---

## Requirements

- Windows 10 or 11

---

## Installation

Install from the [Microsoft Store](https://apps.microsoft.com/detail/FolderMount).

---

## Build from Source

**Prerequisites:** Visual Studio 2022+ with the .NET desktop development workload.

```powershell
# Clone
git clone https://github.com/AshV/FolderMount.git
cd FolderMount

# Build Release
dotnet build FolderMount.sln --configuration Release

# Output
# FolderMount\bin\Release\net10.0-windows\FolderMount.dll
```

---

## How It Works

FolderMount uses the Windows `SUBST` command:
```
SUBST P: "D:\MyProjects"   → creates virtual drive P:
SUBST P: /D                → removes virtual drive P:
```

`SUBST` is a standard Windows command available since Windows XP. It does **not** require administrator privileges.

Mappings are stored at:
```
%APPDATA%\FolderMount\mappings.xml
```

Example file:
```xml
<?xml version="1.0" encoding="utf-8"?>
<FolderMounts>
  <!-- FolderMount mappings — edit with care -->
  <Mapping letter="P" path="D:\Projects" label="Work Projects" />
  <Mapping letter="M" path="E:\Media"    label="Media Library" />
</FolderMounts>
```

---

## Startup

When "Run at Windows startup" is enabled in Settings, FolderMount adds this registry value:

```
HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
  FolderMount = "C:\Users\<you>\AppData\Local\FolderMount\FolderMount.exe" /startup
```

On launch with `/startup`, all saved drives are mounted silently and no window is shown.
`HKCU` does **not** require administrator access.

---

## License

MIT
