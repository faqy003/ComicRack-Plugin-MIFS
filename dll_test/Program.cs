using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;


byte[]? data = File.ReadAllBytes("test.jxl");
IntPtr out_data = IntPtr.Zero;
int w, h ;
w = h = 0;
libfunc.JpegXL_Decoder(data, data.Length, ref out_data, ref w, ref h);
libfunc.Avif_Free(out_data);

Console.WriteLine("");
static class libfunc
{
	[DllImport("libfunc.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "JpegXL_Decoder")]
	internal static extern void JpegXL_Decoder([In] IntPtr data, int dataSize,ref IntPtr output, ref int width, ref int height);

	[DllImport("libfunc.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Avif_Free")]
	internal static extern void Avif_Free([In] IntPtr ptr);
	public unsafe static void JpegXL_Decoder(byte[] data, int dataSize, ref IntPtr output, ref int width, ref int height)
    {
		fixed (byte* ptr = data)
        {
            JpegXL_Decoder((IntPtr)((void*)ptr), dataSize, ref output, ref width,ref height);
        }
    }
}