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

struct PSIn
{
	float4 position : SV_POSITION;
	/*float2 texcoord : TEXCOORD;
	float3 normal : NORMAL;
	float4 color : COLOR;*/
};

float4 VShader(float4 pos : POSITION) : POSITION
{
	return pos;
}

void DrawCube(point PSIn Vertex, inout TriangleStream<PSIn> TriStream)
{
	PSIn Vertices[24];
	for(int i = 0; i < 24; i++)
	{
		Vertices[i] = Vertex;
	}
	// Side 1
	Vertices[0].position += Vert1; // 1
	Vertices[1].position += Vert2; // 2
	Vertices[2].position += Vert8; // 1 // 8?
	Vertices[3].position += Vert3; // 3

	// Side 2
	Vertices[4].position += Vert4; // 4
	Vertices[5].position += Vert5; // 5
	Vertices[6].position += Vert6; // 6
	Vertices[7].position += Vert7; // 7

	// Side 3
	Vertices[8].position += Vert5; // 5
	Vertices[9].position += Vert8; // 8
	Vertices[10].position += Vert7; // 7
	Vertices[11].position += Vert3; // 3

	// Side 4
	Vertices[12].position += Vert4; // 4
	Vertices[13].position += Vert6; // 6
	Vertices[14].position += Vert1; // 1
	Vertices[15].position += Vert2; // 2

	// Side 5
	Vertices[16].position += Vert6; // 6
	Vertices[17].position += Vert7; // 7
	Vertices[18].position += Vert2; // 2
	Vertices[19].position += Vert3; // 3

	// Side 6
	Vertices[20].position += Vert4; // 4
	Vertices[21].position += Vert1; // 1
	Vertices[22].position += Vert5; // 5
	Vertices[23].position += Vert8; // 8

	for(uint u = 0; u < 24; u++)
	{
		Vertices[u].position = mul(Vertices[u].position, WorldViewProj);
		/*
		Vertices[u].color = CalculateColor(Norms[u/4]);
		Vertices[u].normal.xyz = Norms[u/4].xyz;
		Vertices[u].texcoord = UVs[u % 4];
		if(u % 4 == 0 || u % 4 == 2)
		{
			Vertices[u].texcoord.x = 0.125;
			Vertices[u].texcoord.x += 0.125 * floor(u/4);
		}
		else
		{
			Vertices[u].texcoord.x += 0.125 * floor(u/4);
		}*/
	}

	// TextureCount=2
	// if TextureIndex[i] == 0, V = 0.5
	// if TextureIndex[i] == 1, V = 1.0
	// Downwards Vs = (TextureIndex[0]+1/TextureCount)

	// if TextureIndex[i] == 0, V = 0.0
	// if TextureIndex[i] == 1, V = 0.5
	// Topwards Vs  = (TextureIndex[0]/TextureCount)
	
	/*int Index = TextureIndex[ID/4][ID%4];
	Vertices[0].texcoord = float2(0.125, ((float)(Index+1)/TextureCount));
	Vertices[1].texcoord = float2(0,((float)(Index+1)/TextureCount));
	Vertices[2].texcoord = float2(0.125,((float)Index/TextureCount)); // 0.5
	Vertices[3].texcoord = float2(0,((float)Index/TextureCount));	// 0.5

	Vertices[4].texcoord = float2(0.125,((float)(Index+1)/TextureCount));
	Vertices[5].texcoord = float2(0.125,((float)Index/TextureCount));
	Vertices[6].texcoord = float2(0.25,((float)(Index+1)/TextureCount));
	Vertices[7].texcoord = float2(0.25,((float)Index/TextureCount));

	Vertices[8].texcoord = float2(0.375,((float)(Index+1)/TextureCount));
	Vertices[9].texcoord = float2(0.25,((float)(Index+1)/TextureCount));
	Vertices[10].texcoord = float2(0.375,((float)Index/TextureCount));
	Vertices[11].texcoord = float2(0.25,((float)Index/TextureCount));

	Vertices[12].texcoord = float2(0.5,((float)(Index+1)/TextureCount));
	Vertices[13].texcoord = float2(0.375,((float)(Index+1)/TextureCount));
	Vertices[14].texcoord = float2(0.5,((float)Index/TextureCount));
	Vertices[15].texcoord = float2(0.375,((float)Index/TextureCount));

	Vertices[16].texcoord = float2(0.5,((float)(Index+1)/TextureCount));
	Vertices[17].texcoord = float2(0.5,((float)Index/TextureCount));
	Vertices[18].texcoord = float2(0.625,((float)(Index+1)/TextureCount));
	Vertices[19].texcoord = float2(0.625,((float)Index/TextureCount));

	Vertices[20].texcoord = float2(0.75,((float)(Index+1)/TextureCount));
	Vertices[21].texcoord = float2(0.625,((float)(Index+1)/TextureCount));
	Vertices[22].texcoord = float2(0.75,((float)Index/TextureCount));
	Vertices[23].texcoord = float2(0.625,((float)Index/TextureCount));*/

	/* // Orig
	Vertices[0].texcoord = float2(2,2);
	Vertices[1].texcoord = float2(0,1);
	Vertices[2].texcoord = float2(1,0);
	Vertices[3].texcoord = float2(0,0);
	*/

	// Side 1
	TriStream.Append(Vertices[0]);
	TriStream.Append(Vertices[1]);
	TriStream.Append(Vertices[2]);
	TriStream.RestartStrip();

	TriStream.Append(Vertices[2]);
	TriStream.Append(Vertices[1]);
	TriStream.Append(Vertices[3]);
	TriStream.RestartStrip();

	// Side 2
	TriStream.Append(Vertices[4]);
	TriStream.Append(Vertices[5]);
	TriStream.Append(Vertices[6]);
	TriStream.RestartStrip();

	TriStream.Append(Vertices[6]);
	TriStream.Append(Vertices[5]);
	TriStream.Append(Vertices[7]);
	TriStream.RestartStrip();

	// Side 3
	TriStream.Append(Vertices[8]);
	TriStream.Append(Vertices[9]);
	TriStream.Append(Vertices[10]);
	TriStream.RestartStrip();

	TriStream.Append(Vertices[10]);
	TriStream.Append(Vertices[9]);
	TriStream.Append(Vertices[11]);
	TriStream.RestartStrip();

	// Side 4
	TriStream.Append(Vertices[12]);
	TriStream.Append(Vertices[13]);
	TriStream.Append(Vertices[14]);
	TriStream.RestartStrip();

	TriStream.Append(Vertices[14]);
	TriStream.Append(Vertices[13]);
	TriStream.Append(Vertices[15]);
	TriStream.RestartStrip();

	// Side 5
	TriStream.Append(Vertices[16]);
	TriStream.Append(Vertices[17]);
	TriStream.Append(Vertices[18]);
	TriStream.RestartStrip();

	TriStream.Append(Vertices[18]);
	TriStream.Append(Vertices[17]);
	TriStream.Append(Vertices[19]);
	TriStream.RestartStrip();

	// Side 6
	TriStream.Append(Vertices[20]);
	TriStream.Append(Vertices[21]);
	TriStream.Append(Vertices[22]);
	TriStream.RestartStrip();

	TriStream.Append(Vertices[22]);
	TriStream.Append(Vertices[21]);
	TriStream.Append(Vertices[23]);
	TriStream.RestartStrip();
}

[maxvertexcount(36)]
void GShader(point float4 Vertex[1] : POSITION, inout TriangleStream<PSIn> triStream)
{
	PSIn PSVertex;
	PSVertex.position = Vertex[0];
	DrawCube(PSVertex, triStream);
}

float4 PShader() : SV_TARGET
{
	return float4(1.0f, 1.0f, 1.0f, 1.0f);
}

