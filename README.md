# FolderMount

> **Map local folders to virtual drive letters using Windows SUBST — no admin required.**

FolderMount is a polished, lightweight Windows system tray application built with .NET 10 and WPF that allows you to assign virtual drive letters (e.g. `P:`, `M:`) to any folder on your PC. Mappings persist across reboots, can auto-mount silently on login, and require zero administrative privileges.

Published and distributed via the **Microsoft Store** with automatic background updates and sandboxed MSIX security.

---

## Features

| Feature | Details |
|---|---|
| 🗂 **Virtual Drives** | Map any folder → drive letter via native Windows `SUBST` |
| 🔄 **Auto-Mount on Login** | Silent startup via Windows Registry (`HKCU`) — no elevation needed |
| 📋 **XML Persistence** | Mappings safely saved to `%APPDATA%\FolderMount\mappings.xml` |
| 📤 **Export / Import** | Back up or share your mappings across PCs via XML |
| 🟢 **Live Status** | Real-time active/inactive status dot and action buttons per drive |
| ⚡ **Bulk Actions** | Mount All / Disconnect All in one click from main window or tray |
| 📂 **Open in Explorer** | Jump straight to the virtual drive with a single click |
| 🏷️ **Labels** | Assign friendly nicknames to folders |
| ⚙️ **Settings** | Toggle startup, quick access to data folder, and XML backup |
| 🔵 **System Tray** | Runs unobtrusively in the tray with quick-action context menu |
| 🎨 **Dark Theme** | Sleek, custom-crafted dark UI with high-contrast inputs |

---

## Requirements

- **Operating System:** Windows 10 (version 1809 / build 17763 or later) or Windows 11
- **Platform:** x86, x64, or ARM64

---

## Installation

Install directly from the [Microsoft Store](https://apps.microsoft.com/detail/FolderMount):

[![Get it from Microsoft](https://get.microsoft.com/images/en-us%20dark.svg)](https://apps.microsoft.com/detail/FolderMount)

Benefits of the Store version:
- **Clean zero-privilege install** — installs to your user profile without admin prompts.
- **Automatic updates** — always stay on the latest version seamlessly.
- **Clean uninstall** — complete removal via Windows Settings with no leftover registry keys or orphaned files.

---

## Building from Source

### Prerequisites

- [Visual Studio 2022](https://visualstudio.microsoft.com/) (Community edition is fine) with:
  - **.NET desktop development** workload
  - **Universal Windows Platform development** or **Windows Application Packaging Project** tools
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### 1. Build Standalone WPF App

```powershell
# Clone repository
git clone https://github.com/AshV/FolderMount.git
cd FolderMount

# Build Release with .NET CLI
dotnet build FolderMount\FolderMount.csproj --configuration Release

# Output executable located at:
# FolderMount\bin\Release\net10.0-windows\FolderMount.exe
```

### 2. Build MSIX Package with MSBuild

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" FolderMount.sln /p:Configuration=Release /p:Platform="Any CPU"
```

The packaged layout and appx recipe will be generated under `FolderMount.Package\AppPackages\`.

---

## Microsoft Store Publishing Guide

FolderMount uses a **Windows Application Packaging Project** (`FolderMount.Package.wapproj`) to produce standard MSIX packages for Microsoft Partner Center.

### Step 1: Reserve Your App Name in Partner Center
1. Go to the [Microsoft Partner Center Dashboard](https://partner.microsoft.com/dashboard).
2. Under **Apps and games**, click **New product** &rarr; **MSIX or PWA app**.
3. Enter `FolderMount` (or your preferred unique app name) and click **Reserve product name**.

### Step 2: Associate the Solution with the Store in Visual Studio
1. Open `FolderMount.sln` in Visual Studio 2022.
2. In Solution Explorer, right-click **FolderMount.Package**.
3. Select **Publish** &rarr; **Associate App with the Store...**
4. Sign in with your Microsoft Developer account.
5. Select your reserved app name and click **Next** &rarr; **Associate**.
   > This automatically synchronizes `Package.appxmanifest` (Package Name, Publisher ID, Publisher Display Name) and generates `Package.StoreAssociation.xml`.

### Step 3: Create the Store Package (.msixupload)
1. Right-click **FolderMount.Package** &rarr; **Publish** &rarr; **Create App Packages...**
2. Choose **Microsoft Store using a new app name** (or associated existing app name) &rarr; Click **Next**.
3. Select the architectures you want to distribute (e.g. `x64`, `x86`, `ARM64`) and set configuration to **Release**.
4. Check **Generate package bundle** &rarr; **Always**.
5. Click **Create**.
6. Visual Studio builds the solution and generates a `.msixupload` (or `.appxupload`) file in `FolderMount.Package\AppPackages\`.

### Step 4: Validate with Windows App Certification Kit (WACK)
1. At the end of the package creation wizard, click **Launch Windows App Certification Kit**.
2. Ensure all automated validation tests pass (Security tests, App manifest compliance, Supported API checks).

### Step 5: Submit in Partner Center
1. Return to your app submission on [Microsoft Partner Center](https://partner.microsoft.com/dashboard).
2. **Packages:** Drag and drop your `.msixupload` bundle.
3. **Properties:**
   - Category: *Utilities & tools*
   - App declarations: Select *Runs in full trust* (requires the standard `runFullTrust` capability for `SUBST`).
4. **Store listings:**
   - Provide description, release notes, keywords (`subst`, `virtual drive`, `folder mount`, `drive letter`).
   - Upload app screenshots and icons (`FolderMount.Package\Images\StoreLogo.png`).
5. **Pricing and availability:** Set to *Free* and select available markets.
6. Click **Submit to the Store**. Certification typically takes 24 to 72 hours.

---

## How It Works

FolderMount leverages Windows' native `SUBST` utility:
```
SUBST P: "D:\MyProjects"   → creates virtual drive P:
SUBST P: /D                → removes virtual drive P:
```

`SUBST` has been standard in Windows since MS-DOS / Windows XP and operates completely in user space. It does **not** require administrator elevation or kernel-level filesystem drivers.

### Configuration Storage

Mappings are persisted in clean XML at:
```
%APPDATA%\FolderMount\mappings.xml
```

Example format:
```xml
<?xml version="1.0" encoding="utf-8"?>
<FolderMounts>
  <!-- FolderMount mappings — edit with care -->
  <Mapping letter="P" path="D:\Projects" label="Work Projects" mountOnLoad="true" />
  <Mapping letter="M" path="E:\Media"    label="Media Library" mountOnLoad="false" />
</FolderMounts>
```

### Windows Startup Integration

When **Run at Windows startup** is enabled in Settings, FolderMount registers a user-level Run key:
```
HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
  FolderMount = "<ExecutablePath>" /startup
```

On login with the `/startup` flag:
1. The app starts silently minimized in the system tray.
2. Saved mappings flagged with `mountOnLoad="true"` are mounted automatically in a background thread.
3. No disruptive popup windows are displayed on login.

---

## License

MIT &copy; [Ashish Vishwakarma](https://github.com/AshV)
