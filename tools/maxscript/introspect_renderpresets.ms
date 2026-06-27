outPath = @"C:\Users\nguyenbuu.DESKTOP-TOEFTR1\Documents\3ds Max 2022\renderpresets\renderpresets_interface_dump.txt"
outFile = createFile outPath

format "showInterfaces renderPresets\n" to:outFile
showInterfaces renderPresets to:outFile
format "\nshowProperties renderPresets\n" to:outFile
showProperties renderPresets to:outFile

close outFile
print ("Wrote: " + outPath)
