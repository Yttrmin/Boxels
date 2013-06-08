cbuffer PerFrameInfo
{
	float4x4 WorldViewProj : WorldViewProjection;
}

float4 VShader(float4 pos : POSITION) : SV_POSITION
{
	return mul(pos, WorldViewProj);
}

float4 PShader() : SV_TARGET
{
	return float4(1.0f, 1.0f, 1.0f, 1.0f);
}
