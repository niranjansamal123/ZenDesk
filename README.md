<div align="center">

# 🛡️ ZenDesk

**Hide any application from screen sharing — instantly and silently.**

![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-blue?style=flat-square)
![Framework](https://img.shields.io/badge/.NET-8.0-purple?style=flat-square)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)
![Version](https://img.shields.io/badge/Version-v1.0.0-orange?style=flat-square)

</div>

---

## 🖥️ What is ZenDesk?

ZenDesk is a lightweight Windows desktop utility that lets you **hide selected 
application windows from screen capture, screen sharing, and video calls** — 
while still being able to use them normally yourself.

Perfect for interviews, meetings, and presentations where you need privacy 
without closing your apps.

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🙈 **Hide from Capture** | Exclude any app from screen share / recording |
| 🖥️ **Live Preview** | See exactly what others see before you share |
| 🖱️ **Click-Through Mode** | Make windows mouse-transparent |
| 👁️ **Peek Mode** | Temporarily reveal hidden windows with adjustable opacity |
| 🔕 **Taskbar & Tray Hide** | Remove apps from Alt+Tab, taskbar and tray overflow |
| 🌫️ **Transparency Control** | Set custom transparency level for hidden windows |
| ⚡ **Auto-Refresh** | Window list updates every 3 seconds automatically |
| 🎯 **Zero Install** | Single `.exe` — just run and go |

---

## 📦 Installation

### Option 1 — Download Release (Recommended)
1. Go to [Releases](../../releases)
2. Download `ZenDesk.exe`
3. Run directly — **no installation needed**
4. Allow administrator permissions if prompted

### Option 2 — Build from Source
```bash
git clone https://github.com/niranjansamal123/ZenDesk.git
cd ZenDesk
dotnet build -c Release
```

---

## 🚀 How to Use

1. **Launch** `ZenDesk.exe`
2. Check the **Preview** to see what your screen looks like to others
3. In **Hide applications**, tick any window you want to hide
4. Start your screen share — hidden apps are **completely invisible** to viewers
5. Uncheck to restore at any time

---

## ⚙️ Advanced Settings

- **Hide from Alt+Tab and Taskbar** — removes app from task switcher too
- **Show desktop preview** — toggle live preview on/off
- **Make hidden windows transparent** — see through them yourself while hidden
- **Transparency slider** — control opacity from 10% to 90%

---

## 🛠️ Requirements

- Windows 10 / 11 (64-bit)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- Administrator privileges (for DLL injection)

---

## 🏗️ Built With

- **C# / WPF** — UI framework
- **MVVM** architecture
- **Win32 API** — `SetWindowDisplayAffinity`, `WS_EX_TOOLWINDOW`
- **DLL Injection** — for cross-process window property modification
- **.NET 8.0**

---

## ⚠️ Disclaimer

ZenDesk uses the official Windows API `SetWindowDisplayAffinity` 
(`WDA_EXCLUDEFROMCAPTURE`) to hide windows from screen capture.  
It does **not** modify, damage, or interfere with any application's functionality.  
Use responsibly and in accordance with your organization's policies.

---

## 📄 License

This project is licensed under the **MIT License** — see the 
[LICENSE](LICENSE) file for details.

---

<div align="center">
Made with ❤️ — ZenDesk v1.0.0
</div>
