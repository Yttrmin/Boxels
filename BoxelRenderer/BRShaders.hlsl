cbuffer PerFrameInfo
{
	float4x4 WorldViewProj : WorldViewProjection;
}

/** Contains the coordinates to offset each vertex of a Voxel.
Immutable. */
cbuffer BoxelBuffer
{
	/** Coordinates to offset each vertex of a Voxel. */
	float4 Vert1;
	float4 Vert2;
	float4 Vert3;
	float4 Vert4;
	float4 Vert5;
	float4 Vert6;
	float4 Vert7;
	float4 Vert8;
	
	/** The normal for every vertex. */
	float4 Norms[6];
	
	float2 UVs[4];
}