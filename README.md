# descenders-minimap
gives a minimap on your screen of the terrain and position of character<br><br>

This is a work in progress <br><br>

Put OverheadMinimap.cs into Assets/Scripts<br>
Put MinimapPostProcess.shader into Assets/Shader<br>
create a new material and set the shader to Custom/MinimapPostProcess
Create a render texture in your assets window and set the size to 512 x 512 in the inspector window and name it MinimapRT<br>
Create a empty game object and name it MinimapCameraHost and attach this script to it<br>
Minimap Texture is your MinimapRT you created, the minimap camera host is this object itself dragged into that field (MinimapCameraHost)<br>
Drag your new material with the shader into post process material if you would like to brighten up the minimap.

