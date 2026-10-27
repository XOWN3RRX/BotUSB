# BotUSB

BotUSB lets scripts press keys and click or move the mouse **at hardware level**. It sends commands to a
USB stick ("USBautoGAMES" by VNH, VID `0x1606` / PID `0x1988`). Windows sees the stick as a real
keyboard and mouse, so the input is indistinguishable from a person's.

Scripts (UoPilot or anything that can open a TCP socket) send small JSON messages to BotUSB, and BotUSB
forwards them to the stick.

## Requirements and build

- Windows 10 or 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or Visual Studio 2026)

```bash
dotnet build BotUSB.slnx -c Release
```

```bash
dotnet test BotUSB.slnx
```

The app is in `BotUSB/bin/Release/net10.0-windows/BotUSB.exe`. No extra DLLs are needed: the old
`HHID.dll` (a renamed 32-bit mcHID/EasyHID library) is replaced by direct Windows HID calls, so the app
also runs as a 64-bit process.

## The app

- **Log**: everything that happens, with colored levels. *Auto-scroll* follows new messages. *Received
  data* logs every command scripts send. Select rows and press Ctrl+C to copy them.
- **Status**: whether the stick is connected (detected instantly when plugged in or removed), the
  server address and connected scripts, and how long the stick takes per command.
- **Test keys**: send a combination (Alt+Tab, Win+D, Win+Tab, Win+R, Ctrl+Esc, Ctrl+Alt+Del) or a single
  key. *Custom code* accepts a number or a name such as `_Esc`.
- **Test mouse**: click at the cursor, or move the cursor by *step* (1-255).
- **Release all** sends code 0 so nothing stays held. **Test driver** checks that the stick answers.
- **Key codes** (keyboard button): every code with what it does. Double-click one to put it in the
  test box.
- **Settings**: keep running in the tray, always on top, the server port, and whether other computers
  may connect. Settings are saved in `%AppData%\BotUSB\settings.json`.

Only one BotUSB can run at a time; starting it again brings the running window to the front.

## Script protocol

Connect to `127.0.0.1:13000` (the port can be changed in Settings) and send JSON objects. Single or double
quotes both work, and several objects can be sent back to back.

| Message | What happens |
|---|---|
| `{ 'Keys': [58] }` | Taps the key. Tap codes (1-99) are sent once; hold codes are followed by 0 |
| `{ 'Keys': [111], 'Pressed': 'true' }` | Sends the code only: hold codes stay down |
| `{ 'Keys': [0] }` | Releases everything |
| `{ 'Keys': [101, 111] }` | Combination of 2 or 3 keys, then releases them (Shift+A) |
| `{ 'Keys': [207], 'Repeat': 30 }` | Sends the code with the Repeat byte: moves the mouse right by 30 |
| `{ 'Message': 'Hello!' }` | Types the text (US layout) |

Keys can also be given by name: `{ 'Keys': ['_CtrlL', '_C'] }`. Invalid messages are logged and
ignored; they never stop the app.

See [Kbr.txt](Kbr.txt) and [Scripts/Lineage2/L2.txt](Scripts/Lineage2/L2.txt) for UoPilot examples.

## Key codes

These were measured on the stick:

| Codes | Kind | Behaviour |
|---|---|---|
| 0 | `NULL` | Releases every held key and mouse button |
| 1-3 | `MouseLeft`, `MouseRight`, `MouseMiddle` | Click: pressed and released by the stick |
| 4-99 | `A`...`Z`, `Number1`...`Number0`, `F1`...`F12`, arrows, numpad... | Tap: pressed and released by the stick |
| 100-107 | `_CtrlL`, `_ShiftL`, `_AltL`, `_WinL`, `_CtrlR`, `_ShiftR`, `_AltR`, `_WinR` | Hold until 0 |
| 108-110 | `_MouseLeft`, `_MouseRight`, `_MouseLeftRight` | Hold the button(s) until 0 (drag) |
| 111-206 | `_A`...`_Z`, `_1`...`_0`, `_Enter`, `_Esc`, `_F1`...`_F12`... | Hold until 0 |
| 207-210 | `_MouseXPlus`, `_MouseXMinus`, `_MouseYPlus`, `_MouseYMinus` | Move the mouse once by `Repeat` (0 = no move) |

Handy codes: `_Esc` = 148, `_Tab` = 150, `_Space` = 151, `_Enter` = 147, `F1` = 58, `_F1` = 165. The
**Key codes** window lists all of them.

The stick takes about 30 ms per command. A tap code (1-99) is one command, so about 30 taps per second;
a hold code tapped or a combination also needs the release (0), one more command.

> Changed from the original app: it sent 0 after every tap, which also released keys held with
> `'Pressed'`. Now a tap code leaves held keys alone. Send `{ 'Keys': [0] }` to release them.
