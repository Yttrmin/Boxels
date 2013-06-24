#define MAX_BOXEL_TEXTURES 16

struct VSIn
{
	float4 Position : POSITION;
	float3 UVs : TEXCOORD;
};

struct VSOut
{
	float4 Position : SV_POSITION;
	float3 UVs : TEXCOORD;
};

cbuffer PerFrameInfo
{
	float4x4 WorldViewProj : WorldViewProjection;
}

Texture2DArray BoxelTexture;
SamplerState BoxelSamplerState;

VSOut VShaderTextured(VSIn vertex)
{
	VSOut output;
	output.Position = mul(vertex.Position, WorldViewProj);
	output.UVs = vertex.UVs;
	return output;
}

float4 PShaderTextured(VSOut vertex) : SV_TARGET
{
	float4 color = float4(BoxelTexture.Sample(BoxelSamplerState, vertex.UVs));
	if(color.a < .5)
		return float4(0,0,0,0);
	else
		return float4(color.rgb, 1);
}