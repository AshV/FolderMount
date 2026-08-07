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
- .NET Framework 4.8 (pre-installed on all Windows 10/11 systems — **no install needed**)

---

## Installation

### Option A — Installer (Recommended)
Run `FolderMountSetup.exe` — **no administrator rights needed**.
- Installs to `%LOCALAPPDATA%\FolderMount`
- Creates a Start Menu shortcut
- Optionally registers a silent startup entry

### Option B — Portable
Copy `FolderMount.exe` anywhere and run it directly.

---

## Build from Source

**Prerequisites:** Visual Studio 2019+ or MSBuild with .NET Framework 4.8 targeting pack.

```powershell
# Clone
git clone https://github.com/yourname/FolderMount.git
cd FolderMount

# Build Release
msbuild FolderMount.sln /p:Configuration=Release /p:Platform="Any CPU"

# Output
# FolderMount\bin\Release\FolderMount.exe
```

**Build installer** (requires [Inno Setup 6](https://jrsoftware.org/isinfo.php)):
```powershell
iscc Installer\FolderMount.iss
# Output: Installer\Output\FolderMountSetup.exe
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
