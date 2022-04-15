# Modern Image Formats Support for ComicRack 
### Two new formats has been added to ComicRack:
- [AVIF](https://aomediacodec.github.io/av1-avif/) - Better at lossy compression. Same size with best quality. However, it's not that good at high-fidelity & lossless use.
- [JpegXL](https://jpeg.org/jpegxl/) - Better at lossless compression. 30% smaller than PNG image in general. Next generation of jpeg format. Only format that support lossless convert from original jpg.

### Something worthy mentioning about these formats:
- All these formats are still in development and not well & widely supported. 
- Due to the inside mechanics of ComicRack, some new features of these formats are not support. (e.g HDR & animation)

> P.S: I made this project only for practice, not intended for constant maintenance.

# How to use
- Download `.crplugin` file from [Releases](https://github.com/faqy003/ComicRack-Plugin-MIFS/releases) page.
- Drag & drop it on ComicRack.
- Restart your ComicRack. The plugin should enable automatically.

# How it work
You can't add any new format support without touching the core of ComicRack. That can't be done in a ordinary way. But with help of [Harmony](https://github.com/pardeike/Harmony) I can insert my code before or after the original method without actually modify any local file, and make it work like a plugin.

What plugin do is make ComicRack treats all new formats as webp file. When ComicRack processing webp files we check if they are new formats and decode them by ourself.

The pluin must be loaded at the very beginning in order to avoid possible issues. So it using `ReaderResized` hook to trigger its initialisation.

# Plan
- Replace 3rdparty decoder with a better or slim one.
