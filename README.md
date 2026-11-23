# WebP to JPG

WebP to JPG is a Windows desktop app for converting batches of WebP images to JPEG. Drag files into the window, choose the JPEG quality, and convert them locally.

## Build and run

1. Open `WebpToJpg.sln` in Visual Studio with the **.NET desktop development** workload and the **.NET Framework 4.8 targeting pack** installed.
2. Select **Release** and build the solution.
3. Run `bin\Release\WebpToJpg.exe`.

The app uses C#, Windows Forms, and the Windows imaging codecs. It requires .NET Framework 4.8 or later and a Windows codec that can read WebP. If Windows cannot open a valid WebP image, check that **WebP Image Extensions** is installed.

## Convert images

1. Click **Add WebP files…** or drag one or more `.webp` files into the window.
2. To save all results in one folder, click **Browse…**. By default, each JPG is saved alongside its source.
3. Set **JPEG quality (%)** from 1 to 100. The default is 90.
4. Click **Convert**. The **Result** column shows the output filename or an error for each file.

Transparent areas are composited onto white. If `photo.jpg` already exists, the app saves `photo (1).jpg`, then `photo (2).jpg`, and so on. A file that fails to convert does not interrupt the rest of the batch.
