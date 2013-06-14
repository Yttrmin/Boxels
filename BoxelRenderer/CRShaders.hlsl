#define MAX_BOXELS 4096

cbuffer PerFrameInfo
{
	float4x4 WorldViewProj : WorldViewProjection;
}

cbuffer PerInstanceBoxelPositions
{
	float4 InstancePositions[MAX_BOXELS];
}

float4 VShader(float4 pos : POSITION0, float4 posinstance : POSITION1/*, int vertex : SV_VertexID, int instance : SV_InstanceID*/) : SV_POSITION
{
	return mul(pos + posinstance, WorldViewProj);
}

float4 VShaderCBuffer(float4 pos : POSITION, uint instance : SV_InstanceID) : SV_POSITION
{
	return mul(pos + InstancePositions[instance], WorldViewProj);
}

float4 PShader() : SV_TARGET
{
	return float4(1.0f, 1.0f, 1.0f, 1.0f);
}
