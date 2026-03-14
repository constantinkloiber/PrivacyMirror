# PrivacyMirror

[![Download](https://img.shields.io/badge/⬇_Download-PrivacyMirror.exe-blue?style=for-the-badge)](https://github.com/constantinkloiber/PrivacyMirror/releases/latest/download/PrivacyMirror.exe)
[![Handbuch](https://img.shields.io/badge/📖_Handbuch-PDF-grey?style=for-the-badge)](https://github.com/constantinkloiber/PrivacyMirror/raw/main/PrivacyMirror_Handbuch_v1.1.pdf)

> Windows 10/11 · Keine Installation · Einfach starten

> ⚠️ **Windows SmartScreen:** Beim ersten Start erscheint eine Sicherheitswarnung, weil die App kein kommerzielles Zertifikat trägt. Fortfahren mit Klick auf **„Weitere Informationen" → „Trotzdem ausführen"**.

---

**Fenster datenschutzkonform auf einen Beamer spiegeln – der Rest bleibt schwarz.**

PrivacyMirror ist eine kleine Windows-Anwendung für Präsentationen, bei denen der Beamer nur das zeigen soll, was freigegeben wird – und nichts anderes. Dazu wählt der Nutzer ein Fenster aus, das auf den zweiten Monitor (Beamer) gespiegelt wird. Alle anderen Inhalte bleiben unsichtbar.

---

## Features

- 🖥️ **Monitorauswahl** – gezielte Auswahl, welcher Monitor als Beamer-Ausgang genutzt wird
- ⬛ **Schwarzer Hintergrund** – der Beamer zeigt nur das gespiegelte Fenster, sonst nichts
- 🔄 **Live-Spiegelung** – das ausgewählte Fenster wird in Echtzeit gespiegelt (DWM Thumbnail API)
- 🪟 **Fensterliste** – alle sichtbaren Fenster werden aufgelistet und können per Klick ausgewählt werden
- 📐 **Automatische Skalierung** – das Fenster wird proportional auf die Beamer-Auflösung skaliert
- 🧹 **Fenster einsammeln** – verschobene Fenster können mit einem Klick zurück auf den Hauptmonitor geholt werden
- 🚀 **Portable** – keine Installation nötig, einfach `.exe` herunterladen und starten

---

## Voraussetzungen

- Windows 10 oder Windows 11
- Zwei Monitore (Laptop + Beamer oder zwei Bildschirme)

Die Anwendung ist als Self-Contained-EXE veröffentlicht

---

## Download & Verwendung

1. Aktuelle Version unter [**Releases**](../../releases/latest) herunterladen (`PrivacyMirror.exe`)
2. Datei starten – keine Installation nötig
3. Beamer-Monitor in der Dropdown-Liste auswählen
4. **„Monitor reservieren"** klicken → der Beamer zeigt Schwarz
5. Aus der Fensterliste das gewünschte Fenster auswählen
6. **„Spiegelung starten"** klicken → das Fenster erscheint auf dem Beamer
7. Mit **„Spiegelung stoppen"** oder **„Monitor freigeben"** beenden

---

## Aus dem Quellcode bauen

Voraussetzung: [.NET 8 SDK](https://dotnet.microsoft.com/download)

```bash
git clone https://github.com/constantinkloiber/PrivacyMirror.git
cd PrivacyMirror
dotnet build
```

Portable EXE erstellen:

```bash
publish.bat
```

Die fertige EXE liegt dann unter `publish\PrivacyMirror.exe`.

---

## Lizenz

MIT License – siehe [LICENSE](LICENSE)
