#pragma once

#define DllExport   __declspec( dllexport )
extern "C" DllExport void JpegXL_Decoder(BYTE * in_data, int in_size, BYTE * *out_data, int* out_w, int* out_h);
extern "C" DllExport void Avif_Decoder(BYTE * in_data, int in_size, BYTE * *out_data, int* out_w, int* out_h, UINT * out_stride);
extern "C" DllExport void Avif_Free(LPVOID in_ptr);