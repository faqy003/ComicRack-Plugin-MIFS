using HarmonyLib;
using cYo.Common.IO;
using cYo.Projects.ComicRack.Engine.IO;
using cYo.Projects.ComicRack.Engine.IO.Provider;
using cYo.Projects.ComicRack.Engine.IO.Provider.Readers;
using cYo.Projects.ComicRack.Plugins;
//using cYo.Projects.ComicRack.Viewer;
using System.Drawing;
using System.Drawing.Imaging;
using cYo.Common.Drawing;
using System.Text;
using System.Runtime.InteropServices;

namespace ComicRack.Plugin.Avif
{
    public class Patchs
    {
        public static string Id = "com.comicrack.mifs";
        private static Harmony harmony = new Harmony(Id);
        private static bool patched = false;
        public static void DoPatching()
        {
            if (patched)return;
            harmony.PatchAll();
            patched = true;
            Log("- Modern Image Formats Support Loaded");
        }
        public static void Log(string s)
        {
            PythonCommand.LogDefault(s, "");
        }
        static class Imports
        {
            [DllImport("libfunc.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Avif_Decoder")]
            internal static extern void Avif_Decoder([In] IntPtr data, int dataSize, ref IntPtr output, ref int width, ref int height, ref uint stride);

            [DllImport("libfunc.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Avif_Free")]
            internal static extern void Avif_Free([In] IntPtr ptr);
            public unsafe static void Avif_Decoder(byte[] data, int dataSize, ImageInfo imageInfo)
            {
                fixed (byte* ptr = data)
                {
                    Avif_Decoder((IntPtr)((void*)ptr), dataSize, ref imageInfo.output, ref imageInfo.Width, ref imageInfo.Height, ref imageInfo.Stride);
                }
            }
            [DllImport("libfunc.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "JpegXL_Decoder")]
            internal static extern void JpegXL_Decoder([In] IntPtr data, int dataSize, ref IntPtr output, ref int width, ref int height);
            public unsafe static void JpegXL_Decoder(byte[] data, int dataSize, ImageInfo imageInfo)
            {
                fixed (byte* ptr = data)
                {
                    JpegXL_Decoder((IntPtr)((void*)ptr), dataSize, ref imageInfo.output, ref imageInfo.Width, ref imageInfo.Height);
                }
            }
        }
        class ImageInfo
        {
            public int Width = 0;
            public int Height = 0;
            public uint Stride = 0;
            public IntPtr output = IntPtr.Zero;
        }
        public class Avif
        {
            private readonly static byte[] header = Encoding.ASCII.GetBytes("    ftypavif");
            public static bool HeaderCheck(byte[] data)
            {
                if(data.Length < 32) return false;

                for (int i = 4; i < header.Length; i++)
                {
                    if (data[i] != header[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            public static byte[] Decode(byte[] data)
            {
                var info = new ImageInfo();
                Bitmap? bitmap = null;
                byte[]? result = null;

                try
                {
                    Imports.Avif_Decoder(data, data.Length, info);
                    if(info.output == IntPtr.Zero)
                    {
                        throw new Exception("bad avif file");
                    }
                    bitmap = new Bitmap(info.Width, info.Height,(int)info.Stride, PixelFormat.Format32bppArgb, info.output);
                    result = bitmap.ImageToJpegBytes(-1);
                }
                finally
                {
                    if (bitmap != null) bitmap.Dispose();
                    if (info.output != IntPtr.Zero) Imports.Avif_Free(info.output);
                }
                if(result != null) return result;
                return data;
            }
        }
        public class JpegXL
        {
            private readonly static byte[] header = Encoding.ASCII.GetBytes("\x00\x00\x00\x0CJXL \x0D\x0A\x87\x0A");
            public static bool HeaderCheck(byte[] data)
            {
                if (data.Length < 12) return false;

                if (data[0] == 0xff && data[1] == 0x0A) return true;

                for (int i = 0; i < header.Length; i++)
                {
                    if (data[i] != header[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            public static byte[] Decode(byte[] data)
            {
                var info = new ImageInfo();
                Bitmap? bitmap = null;
                byte[]? result = null;
                try
                {
                    Imports.JpegXL_Decoder(data, data.Length, info);
                    if (info.output == IntPtr.Zero)
                    {
                        throw new Exception("bad jxl file");
                    }
                    bitmap = new Bitmap(info.Width, info.Height, info.Width*4, PixelFormat.Format32bppArgb, info.output);
                    result = bitmap.ImageToJpegBytes(-1);
                }
                finally
                {
                    if (bitmap != null) bitmap.Dispose();
                    if (info.output != IntPtr.Zero) Imports.Avif_Free(info.output);
                }
                if (result != null) return result;
                return data;
            }
        }

        [HarmonyPatch(typeof(ComicProvider))]
        [HarmonyPatch("IsSupportedImage")]
        public static class Patch01
        {
            public static void Postfix(string file, ref bool __result)
            {
                if (__result) return;
                string fileExt = Path.GetExtension(FileUtility.MakeValidFilename(file, '_'));
                __result = supportedTypes.Any((string ext) => string.Equals(fileExt, "." + ext, StringComparison.OrdinalIgnoreCase));
            }

            private static readonly string[] supportedTypes = new string[]
            {
            "avif",
            "jxl"
            };
        }

        [HarmonyPatch(typeof(StorageProvider))]
        [HarmonyPatch("GetStoragePageTypeFromExtension")]
        public static class Patch02
        {
            public static bool Prefix(string ext, ref StoragePageType __result)
            {
                string text = (ext ?? string.Empty).ToLower();
                if (text == ".avif" || text == ".jxl")
                {
                    __result = StoragePageType.Webp;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(WebpImage))]
        [HarmonyPatch(nameof(WebpImage.ConvertToJpeg))]
        public static class Patch03
        {
            static bool Prefix(byte[] data, ref byte[] __result)
            {
                if (Avif.HeaderCheck(data))
                {
                    __result = Avif.Decode(data);
                    if (__result != data) return false;
                }else if (JpegXL.HeaderCheck(data)) {
                    __result = JpegXL.Decode(data); 
                    if (__result != data) return false;
                }
                return true;
            }
        }
    }
}