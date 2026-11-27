# Color Image Tool (Independent Compatibility Implementation)

[简体中文](README.md) | [日本語](README.ja.md)

A small Windows desktop application that extracts files from PNG “colorful pictures” and embeds files into PNG images. When multiple files are selected for embedding, they are first packaged into a ZIP archive; extract the ZIP after decoding.

## Project origin

This is not the official `TT-tools` project, and it is not a direct build of the full `TT-tools` repository. The UI and source in this repository were written as an independent, standalone compatibility implementation.

The implementation work consulted the legacy, password-free colorful-image flow in the public [linyi1111123/TT-tools](https://github.com/linyi1111123/TT-tools) repository to identify the data layout and compatibility target:

- [`tt_img_enc_node.py`](https://github.com/linyi1111123/TT-tools/blob/master/tt_img_enc_node.py): single/multiple image handling and payload header layout.
- [`tt_img_utils.py`](https://github.com/linyi1111123/TT-tools/blob/master/tt_img_utils.py): writing data into the least significant bits of RGB pixels.
- [`tt_img_dec_node.py`](https://github.com/linyi1111123/TT-tools/blob/master/tt_img_dec_node.py): reading data back from pixel least significant bits.

No source files from that repository were copied into this project. This program targets only the referenced legacy, password-free RGB-LSB format. It may not support other versions, password-protected formats, or images produced by other tools. The original repository is primarily a set of ComfyUI custom nodes; this project does not include ComfyUI.

## Features

- Batch extraction from PNG images.
- Embed one file into a PNG image.
- Package multiple files as ZIP before embedding them into a PNG image.
- Ctrl/Shift multi-selection and batch removal in the file list.
- Restrict extracted extensions to letters and digits; invalid extensions are saved as `.bin`.

## Build

Requires Windows, the .NET 8 SDK, and the Windows Desktop Runtime. The project has no third-party NuGet dependencies.

```powershell
dotnet publish .\ColorImageTool.csproj -c Release --self-contained false -o .\publish
```

The resulting application requires the .NET 8 Desktop Runtime on the target computer.

## Format and limitations

- Input carrier images must be PNG. Lossy formats such as JPEG, resizing, or color adjustments can damage embedded data.
- A single input file is stored byte-for-byte with its original extension. Multiple files are stored as a `.zip` payload.
- This format primarily hides/packages data in image pixels; it is not reliable encryption. Do not use it to protect sensitive information.
- No open-source license is included with this repository.

