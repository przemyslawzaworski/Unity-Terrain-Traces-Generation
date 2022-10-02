# Unity Terrain Traces Generation

Author: Przemyslaw Zaworski
 
Licence: Creative Commons Attribution 3.0 Unported License https://creativecommons.org/licenses/by/3.0/

Open the scene: Scenes/Main. Play. Tested with Unity 2022.1.18

To support many Terrain Components, create separate materials for every of them from shader "Nature/Terrain/Terrain Traces Standard",

then assign them into array TerrainTracesGeneration.Terrains

Terrains should not overlap in Y axis, otherwise you will need to create separate TerrainTracesGeneration components for them

Features:

* GPU-based collision detection between terrains and emitters

* Optional fade out effect for traces

![alt text](Image1.gif)


![alt text](Image2.gif)

