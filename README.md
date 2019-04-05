# BuildTurboCdPcePkg
Build the pce.pkg to use in Turbo CD injections

Example usage:  
BuildTurboCdPcePkg.exe "Directory"  
BuildTurboCdPcePkg.exe "..\Directory"  
BuildTurboCdPcePkg.exe "C:\Users&#92;&#60;UserName&#62;&#92;Documents\Directory"

The directory you are using to build the pce.pkg file should contain the following:  
-Exactly one &#42;.hcd file  
-One to many &#42;.ogg files  
-One to many &#42;.bin files  

All *.ogg files should be resampled at 32000Hz, otherwise the audio will play incorrectly
