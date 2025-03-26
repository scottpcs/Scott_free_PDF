# Scott's Free PDF Viewer

A lightweight, high-performance PDF viewer built with C# and WPF that offers excellent rendering quality for detailed PDFs.

## Features

- **High-Quality Rendering**: 600 DPI rendering with enhanced text clarity
- **Smooth Zooming**: Responsive zoom controls with percentage display
- **Efficient Navigation**: Easy page-by-page navigation with direct page access
- **Memory Optimized**: Designed to handle large documents efficiently
- **Clean UI**: Intuitive interface with standard controls familiar to PDF users

## Prerequisites

- Windows OS
- .NET 6.0 SDK or later
- Visual Studio 2022 or VS Code with C# extensions

## Installation

### Clone and Build

1. Clone this repository or download the source code
2. Open a terminal in the project directory
3. Build the application:
   ```
   dotnet restore
   dotnet build
   ```
4. Run the application:
   ```
   dotnet run
   ```

### Using Visual Studio

1. Open the solution file in Visual Studio
2. Build the solution (F6)
3. Run the application (F5)

## Usage

### Basic Controls

- **Open PDF**: Click the "Open" button and select a PDF file
- **Navigate**: Use the "<" and ">" buttons to move between pages
- **Zoom**: Use "+" and "-" buttons or click "100%" to reset zoom
- **Go to Page**: Use View > Go to Page to jump to a specific page

### Keyboard Shortcuts

- **Page Down/Up**: Next/Previous page
- **Ctrl++/Ctrl+-**: Zoom in/out
- **Ctrl+0**: Reset zoom to 100%

## Technical Details

This application uses:

- **WPF (Windows Presentation Foundation)** for the UI
- **PdfiumViewer** for PDF rendering
- **System.Drawing.Common** for image processing
- **MVVM pattern** for clean separation of concerns

The rendering pipeline uses a two-step process:
1. High-DPI rendering of PDF content (600 DPI)
2. High-quality scaling with advanced interpolation

## Limitations

- **Editing**: Currently view-only, with placeholder buttons for editing features
- **Annotations**: Text annotation feature is not implemented yet
- **Printing**: No built-in printing functionality
- **Forms**: Interactive PDF forms are displayed but not functional

## Future Improvements

- Implement text annotations
- Add form filling capability
- Add printing support
- Improve memory efficiency for very large documents
- Add search functionality
- Add bookmarks panel

## License

This project uses the PdfiumViewer library which is subject to its own licensing terms.

---

Created by Scott - 2025