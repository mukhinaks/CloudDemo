
struct BATCH {
	float4x4	Projection		;
	float4x4	View			;
	float4x4	World			;
	
	float4		CameraPos		;
	
	float4		SkyLightDir		;
	float4		SkyLightColor	;
	
	float4		LightPos0		;
	float4		LightPos1		;
	float4		LightPos2		;
	float4		LightColor0		;
	float4		LightColor1		;
	float4		LightColor2		;
};

struct VS_IN {
	float3 Position : POSITION;
	float3 Normal 	: NORMAL;
	float4 Color 	: COLOR;
	float2 TexCoord : TEXCOORD0;
	float  Size		: TEXCOORD1;
	float  Angle	: TEXCOORD2;
};

struct OUT_PARTICLE {
	float4	Position	: POSITION;
	float4	Color		: COLOR;
	float2	TexCoord	: TEXCOORD0;
	float	Size		: TEXCOORD1;
	float   Angle		: TEXCOORD2;
	float3	WNormal		: NORMAL;
};

struct PS_IN {
	float4 	Position 	: SV_POSITION;
	float4 	Color 		: COLOR;
	float2 	TexCoord 	: TEXCOORD0;
	float3	WNormal		: TEXCOORD1;
};


cbuffer 		CBBatch 	: 	register(b0) { BATCH Batch : packoffset( c0 ); }	
SamplerState	Sampler		: 	register(s0);
Texture2D		Texture 	: 	register(t0);

#if 0
$ubershader RELATIVE|FIXED
#endif
 
/*-----------------------------------------------------------------------------
	Shader functions :
-----------------------------------------------------------------------------*/
OUT_PARTICLE VSMain( VS_IN input )
{
	OUT_PARTICLE output 	= (OUT_PARTICLE)0;

	float4 	pos		=	float4( input.Position, 1 );
#ifdef FIXED
	float4	wPos	=	mul( pos,  Batch.World 		);
	float4	vPos	=	mul( pos, Batch.View 		);
#endif

#ifdef RELATIVE
	float4	vPos	=	mul( pos + Batch.CameraPos, Batch.View 		);
#endif
	//float4	pPos	=	mul( vPos, Batch.Projection );
	float4	normal	=	mul( float4(input.Normal,0),  Batch.World 		);
	
	output.Position = vPos;
	float c = 1;//Noise.SampleLevel(Sampler, float2(pos.x, pos.z)/100, 0);

	if (c < 0.3f) {
		output.Color	= 0;
		output.Size		= 0;
	} else {
		output.Color 	= input.Color * c;
		output.Size		= input.Size / 2;
	}
	//output.Color 	= input.Color ;//* Noise[float2(input.Position.x + 512, input.Position.z + 512)];
	output.TexCoord	= input.TexCoord;
	output.WNormal	= normalize(normal);
	uint2 pos_xy = { 0, 0 } ;
	/// * Noise.SampleLevel(Sampler, float2(pos.x, pos.z)/100, 0); //pow(Noise.SampleLevel(Sampler, float2(pos.x, pos.z), 0), 2);
	output.Angle	= 0;//input.Angle;
	//output.Size		= input.Size / 2;
	return output;
}

[maxvertexcount(6)]
void GSMain( point OUT_PARTICLE inputPoint[1], inout TriangleStream<PS_IN> outputStream )
{
	PS_IN p0, p1, p2, p3;
	
	OUT_PARTICLE prt = inputPoint[0];	
	
	float  sz 		=   prt.Size;	
	float4 color	=	prt.Color;
	float4 pp		=	prt.Position;
//	float4 pp		=	mul(position, Batch.View); 

	float  a		=	prt.Angle;	
	float2x2	m	=	float2x2( cos(a), sin(a), -sin(a), cos(a) );
	
	p0.Position	= mul( pp + float4( mul(float2( sz, sz), m), 0, 0), Batch.Projection );
	p0.TexCoord	= float2(1,1);
	p0.Color 	= color;
	p0.WNormal	= prt.WNormal;
	
	p1.Position	= mul( pp + float4( mul(float2(-sz, sz), m), 0, 0), Batch.Projection );
	p1.TexCoord	= float2(0,1);
	p1.Color 	= color;
	p1.WNormal	= prt.WNormal;
	
	p2.Position	= mul( pp + float4( mul(float2(-sz,-sz), m), 0, 0), Batch.Projection );
	p2.TexCoord	= float2(0,0);
	p2.Color 	= color;
	p2.WNormal	= prt.WNormal;
	
	p3.Position	= mul( pp + float4( mul(float2( sz,-sz), m), 0, 0), Batch.Projection );
	p3.TexCoord	= float2(1,0);
	p3.Color 	= color;
	p3.WNormal	= prt.WNormal;

	outputStream.Append(p0);
	outputStream.Append(p1);
	outputStream.Append(p2);
	
	outputStream.RestartStrip();

	outputStream.Append(p0);
	outputStream.Append(p2);
	outputStream.Append(p3);

	outputStream.RestartStrip();
}

float4 PSMain( PS_IN input ) : SV_Target
{
	return Texture.Sample( Sampler, input.TexCoord ) * 0.5 * input.Color;
}




