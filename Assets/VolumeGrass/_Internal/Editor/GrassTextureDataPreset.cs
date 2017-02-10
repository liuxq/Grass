using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.IO;
using System.Text;

[System.Serializable]
public class GrassTextureDataPreset : ScriptableObject {
	[SerializeField] public int numSlices;
	[SerializeField] public int numSourceBladesPerSlice;
	[SerializeField] public int numSourceBladesOnBackground;
	[SerializeField] public int sliceHeight;
	[SerializeField] public int textureWidth;
	[SerializeField] public Color BackColor;
	[SerializeField] public int randomSeed;
	[SerializeField] public float fakeSelfShadowStrength;
	[SerializeField] public float randomYOffset;
	[SerializeField] public static float backAODarkening=0.4f;
	
	// source textures and their properties
	[SerializeField] public Texture2D[] grassBlades;
	[SerializeField] public Color[] grassBladesTints;
	[SerializeField] public float[] grassBladesSaturations;
	[SerializeField] public int[] grassBladesWeights;
}
