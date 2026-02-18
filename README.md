# descenders-minimap
gives a minimap on your screen of the terrain and position of character<br><br>

This is a work in progress <br><br>

Put OverheadMinimap.cs into Assets/Scripts<br>
Create a render texture in your assets window and set the size to 512 x 512 in the inspector window and name it MinimapRT<br>
Create a empty game object and name it MinimapCameraHost and attach this script to it<br>
Minimap Texture is your MinimapRT you created, the minimap camera host is this object itself dragged into that field (MinimapCameraHost) 
