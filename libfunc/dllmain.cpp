// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "pch.h"
#include "dllmain.h"
#include <avif/avif.h>
#include <jxl/decode_cxx.h>
#include <jxl/resizable_parallel_runner_cxx.h>
#include <vector>

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

extern "C" DllExport void JpegXL_Decoder(BYTE * in_data, int in_size, BYTE * *out_data, int* out_w, int* out_h) {
    auto runner = JxlResizableParallelRunnerMake(nullptr);
    
    auto dec = JxlDecoderMake(nullptr);
    if (JXL_DEC_SUCCESS != JxlDecoderSubscribeEvents(dec.get(), JXL_DEC_BASIC_INFO /*| JXL_DEC_COLOR_ENCODING*/ | JXL_DEC_FULL_IMAGE)) {
        return;
    }

    if (JXL_DEC_SUCCESS != JxlDecoderSetParallelRunner(dec.get(),JxlResizableParallelRunner,runner.get())) {
        return;
    }

    LPVOID pixels_buffer = NULL;
    //std::vector<uint8_t> icc_profile;
    JxlBasicInfo info;
    JxlPixelFormat format = { 4, JXL_TYPE_UINT8, JXL_NATIVE_ENDIAN, 0 };

    JxlDecoderSetInput(dec.get(), in_data, in_size);
    //JxlDecoderReleaseInput(dec.get());


    for (;;) {
        JxlDecoderStatus status = JxlDecoderProcessInput(dec.get());
        if (status == JXL_DEC_ERROR || status == JXL_DEC_NEED_MORE_INPUT) {
            return;
        }
        else if (status == JXL_DEC_BASIC_INFO) {
            if (JXL_DEC_SUCCESS != JxlDecoderGetBasicInfo(dec.get(), &info)) {
                return;
            }
            *out_w = info.xsize;
            *out_h = info.ysize;
            JxlResizableParallelRunnerSetThreads( runner.get(),JxlResizableParallelRunnerSuggestThreads(info.xsize, info.ysize));
        }
        //else if (status == JXL_DEC_COLOR_ENCODING) {
        //    // Get the ICC color profile of the pixel data
        //    size_t icc_size;
        //    if (JXL_DEC_SUCCESS !=JxlDecoderGetICCProfileSize(dec.get(), &format, JXL_COLOR_PROFILE_TARGET_DATA, &icc_size)) {
        //        return;
        //    }
        //    icc_profile.resize(icc_size);
        //    if (JXL_DEC_SUCCESS != JxlDecoderGetColorAsICCProfile(dec.get(), &format,JXL_COLOR_PROFILE_TARGET_DATA,icc_profile.data(), icc_profile.size())) {
        //        return;
        //    }
        //}
        else if (status == JXL_DEC_NEED_IMAGE_OUT_BUFFER) {
            size_t buffer_size;
            if (JXL_DEC_SUCCESS !=JxlDecoderImageOutBufferSize(dec.get(), &format, &buffer_size)) {
                return;
            }
            if (buffer_size !=  * out_w * *out_h * 4) {
                return;
            }
            size_t pixels_buffer_size = *out_w * *out_h * 4;
            pixels_buffer = avifAlloc(pixels_buffer_size);
            if (JXL_DEC_SUCCESS != JxlDecoderSetImageOutBuffer(dec.get(), &format,pixels_buffer,pixels_buffer_size)) {
                avifFree(pixels_buffer);
                return ;
            }
        }
        else if (status == JXL_DEC_SUCCESS || status == JXL_DEC_FULL_IMAGE) {
            //BGR -> RGB
            for (int i = 0; i < *out_w * *out_h *4; i += 4) {
                BYTE b = ((LPBYTE)pixels_buffer)[i];
                ((LPBYTE)pixels_buffer)[i] = ((LPBYTE)pixels_buffer)[i + 2];
                ((LPBYTE)pixels_buffer)[i + 2] = b;
            }

            *out_data = (LPBYTE)pixels_buffer;
            return;
        }
        else {
            return;
        }
    }
}

extern "C" DllExport void Avif_Decoder(BYTE* in_data,int in_size,BYTE** out_data,int* out_w,int* out_h,UINT* out_stride) {
    avifRGBImage rgb = { 0 };

    avifDecoder* decoder = avifDecoderCreate();
    decoder->codecChoice = AVIF_CODEC_CHOICE_DAV1D;

    avifResult result = avifDecoderSetIOMemory(decoder, in_data, in_size);
    if (result != AVIF_RESULT_OK) goto cleanup;

    result = avifDecoderParse(decoder);
    if (result != AVIF_RESULT_OK) goto cleanup;

    result = avifDecoderNextImage(decoder);
    if (result != AVIF_RESULT_OK) goto cleanup;

    avifRGBImageSetDefaults(&rgb, decoder->image);
    rgb.depth = 8;
    rgb.format = AVIF_RGB_FORMAT_BGRA;

    avifRGBImageAllocatePixels(&rgb);
    result = avifImageYUVToRGB(decoder->image, &rgb);
    if (result != AVIF_RESULT_OK) { 
        avifRGBImageFreePixels(&rgb);
        goto cleanup; }

    *out_data = rgb.pixels;
    *out_w = rgb.width;
    *out_h = rgb.height;
    *out_stride = rgb.rowBytes;

cleanup:
    avifDecoderDestroy(decoder);
}

DllExport void Avif_Free(LPVOID in_lp)
{
    avifFree(in_lp);
}
