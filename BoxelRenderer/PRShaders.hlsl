struct VSIn
{
	float4 Position : POSITION;
	float2 UVs : TEXCOORD;
};

struct VSOut
{
	float4 Position : SV_POSITION;
	float2 UVs : TEXCOORD;
};

cbuffer PerFrameInfo
{
	float4x4 WorldViewProj : WorldViewProjection;
}

Texture2D BoxelTexture;
SamplerState BoxelSamplerState;

float4 VShader(float4 pos : POSITION) : SV_POSITION
{
	return mul(pos, WorldViewProj);
}

VSOut VShaderTextured(VSIn vertex)
{
	VSOut output;
	output.Position = mul(vertex.Position, WorldViewProj);
	output.UVs = vertex.UVs;
	return output;
}

float4 PShader() : SV_TARGET
{
	return float4(1.0f, 1.0f, 1.0f, 1.0f);
}

float4 PShaderTextured(VSOut vertex) : SV_TARGET
{
	return float4(BoxelTexture.Sample(BoxelSamplerState, vertex.UVs));
}