import clr
clr.AddReferenceToFile("0Harmony.dll")
clr.AddReferenceToFile("ComicRack.Plugin.Avif.dll")
from ComicRack.Plugin.Avif import Patchs

#@Name Modern Image Formats Support
#@Hook ReaderResized
#@Enabled false
#@Description Avif Support
def OnStartup(*args):
	Patchs.DoPatching()