# WebP ↔ JPG

<img src="Assets/WebpToJpg.png" width="96" height="96" alt="Landscape photo with an orange conversion arrow">

WebpToJpg is a Windows desktop app for batch conversion between WebP and JPEG. Choose a direction, drag your images into the window and set the output quality. All conversion runs locally.

## Build and run

1. Open `WebpToJpg.sln` in Visual Studio with the **.NET desktop development** workload and the **.NET Framework 4.8 targeting pack** installed.
2. Select **Release** and build the solution.
3. Run `bin\Release\WebpToJpg.exe`. When copying the app, keep the `Tools` folder from the build output next to the EXE.

The app uses C#, Windows Forms, and .NET Framework 4.8. WebP → JPG requires a Windows codec that can read WebP. If Windows cannot open a valid WebP image, check that **WebP Image Extensions** is installed.

JPG → WebP uses the bundled `cwebp.exe` from libwebp 1.6.0. It needs a Windows system capable of running x64 programs. JPEG orientation is applied to the output pixels. The encoder and its license notices are in `Tools\WebP`. [Upstream Windows x64 archive](https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.6.0-windows-x64.zip)

## Convert images

1. Select **WebP → JPG** or **JPG → WebP**. Click **Add WebP files…** or **Add JPEG files…**, or drag matching files into the window. JPEG input accepts both `.jpg` and `.jpeg`. Each direction keeps its own queue.
2. To save all results in one folder, click **Browse…**. By default, each output is saved alongside its source.
3. Set the output quality from 1 to 100. The default is 90.
4. Click **Convert**. The **Result** column shows the output filename or an error for each file.

When converting to JPEG, transparent areas are composited onto white. Both directions append a number to existing filenames, for example `photo (1).webp` or `photo (1).jpg`. A file that fails to convert does not interrupt the rest of the batch.
