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

Texture2DArray BoxelTextures : register(t0);
Texture3D<uint> TextureLookup : register(t1);
SamplerState BoxelSamplerState : register(s0);

VSOut VShaderTextured(VSIn vertex)
{
	VSOut output;
	output.Position = mul(vertex.Position, WorldViewProj);
	output.UVs = vertex.UVs;
	return output;
}

float4 PShaderTextured(VSOut vertex) : SV_TARGET
{
	float4 color = float4(BoxelTextures.Sample(BoxelSamplerState, vertex.UVs));
	if (color.a < .5)
		return float4(0, 0, 0, 0);
	else
		return float4(color.rgb, 1);
}

/*float4 PShaderTextured(VSOut vertex) : SV_TARGET
{
	float4 color = float4(BoxelTextures.Sample(BoxelSamplerState, 
		float3(vertex.UVs.xy, TextureLookup.Load(int4(vertex.UVs.xyz, 0)))));
	if(color.a < .5)
		return float4(0,0,0,0);
	else
		return float4(color.rgb, 1);
}*/