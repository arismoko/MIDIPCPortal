---

# MIDI PC Portal

MIDI PC Portal is a simple application that allows you to bind MIDI note events to launching applications. It uses the Raylib library for its graphical user interface and Melanchall's DryWetMIDI library to handle MIDI events.

## Features

- Select a MIDI input device.
- Bind MIDI note events to launch specific applications.
- Save and load bindings between sessions.

## Prerequisites

- .NET 5.0 or higher
- Raylib_cs
- Melanchall.DryWetMIDI

## Installation

1. Clone the repository:

```sh
git clone https://github.com/yourusername/MIDI-PC-Portal.git
cd MIDI-PC-Portal
```

2. **Important:** Delete the `data.json` file before running the application for the first time to avoid any conflicts with pre-existing configurations:

```sh
del data.json  # or rm data.json on Linux/Mac
```

3. Build and run the application:

```sh
dotnet build
dotnet run
```

## Usage

1. Select your MIDI device by clicking the "Select Device" button.
2. Enter the path to the application you want to launch when a MIDI note is pressed.
3. Press the "Bind Note to App?" button and then press the MIDI note you want to bind.
4. The application will launch the specified app whenever the bound MIDI note is pressed.

## Contributing

Feel free to submit issues or pull requests if you have any suggestions or improvements.

## License

This project is licensed under the MIT License.

---

Feel free to adjust the repository URL, author details, or any other information as needed.
