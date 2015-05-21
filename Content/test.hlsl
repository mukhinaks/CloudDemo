
#if 0
$ubershader INJECTION|SIMULATION|DRAW
#endif

#define BLOCK_SIZE	512

struct PARTICLE {
	float3	Position;
	float3	Velocity;
	//float3	Acceleration;
	float4	Color0;
	float	Size0;
	
};

struct PARAMS {
	float4x4	View;
	float4x4	Projection;
	int			MaxParticles;
	float		DeltaTime;
};

cbuffer CB1 : register(b0) { 
	PARAMS Params; 
};

SamplerState						Sampler				: 	register(s0);
Texture2D							Texture 			: 	register(t0);
StructuredBuffer<PARTICLE>			particleBufferSrc	: 	register(t1);
AppendStructuredBuffer<PARTICLE>	particleBufferDst	: 	register(u0);

/*-----------------------------------------------------------------------------
	Simulation :
-----------------------------------------------------------------------------*/
#if (defined INJECTION) || (defined SIMULATION)
[numthreads( BLOCK_SIZE, 1, 1 )]
void CSMain( 
	uint3 groupID			: SV_GroupID,
	uint3 groupThreadID 	: SV_GroupThreadID, 
	uint3 dispatchThreadID 	: SV_DispatchThreadID,
	uint  groupIndex 		: SV_GroupIndex
)
{
	int id = dispatchThreadID.x;

#ifdef INJECTION
	if (id < Params.MaxParticles) {
		PARTICLE p = particleBufferSrc[ id ];
		
			particleBufferDst.Append( p );
		
	}
#endif

#ifdef SIMULATION
	if (id < Params.MaxParticles) {
		PARTICLE p = particleBufferSrc[ id ];
		
			particleBufferDst.Append( p );
		
	}
#endif
}
#endif


/*-----------------------------------------------------------------------------
	Rendering :
-----------------------------------------------------------------------------*/

struct VSOutput {
	int vertexID : TEXCOORD0;
};

struct GSOutput {
	float4	Position : SV_Position;
	float2	TexCoord : TEXCOORD0;
	float2	TexCoord1 : TEXCOORD1;
	float 	TexCoord2 : TEXCOORD2;
	float3	TexCoord3 : TEXCOORD3;
	float4	Color    : COLOR0;

};


#if DRAW
VSOutput VSMain( uint vertexID : SV_VertexID )
{
	VSOutput output;
	output.vertexID = vertexID;
	return output;
}


float VelocityRecountY(float size) 
{
	float y = 1;
	
	y = 2 * 9.8f * pow (size, 2) * pow(10, -12) * (500 - 1.2041f) / 9 / 1.78 / pow(10, -5);
	
	return y;
}



[maxvertexcount(6)]
void GSMain( point VSOutput inputPoint[1], inout TriangleStream<GSOutput> outputStream )
{
	GSOutput p0, p1, p2, p3;
	
	p0.TexCoord1 = 0; p0.TexCoord2 = 0; p0.TexCoord3 = 0; //p0.Color1 = 0;
	p1.TexCoord1 = 0; p1.TexCoord2 = 0; p1.TexCoord3 = 0; //p1.Color1 = 0;
	p2.TexCoord1 = 0; p2.TexCoord2 = 0; p2.TexCoord3 = 0; //p2.Color1 = 0;
	p3.TexCoord1 = 0; p3.TexCoord2 = 0; p3.TexCoord3 = 0; //p3.Color1 = 0;
	
	PARTICLE prt = particleBufferSrc[ inputPoint[0].vertexID ];
	
	
	
	//float factor	=	saturate(prt.LifeTime / prt.TotalLifeTime);
	
	float  sz 		=   prt.Size0 ; //lerp( prt.Size0, prt.Size1, factor )/2;
	//float  time		=	prt.LifeTime;
	float4 color	=	prt.Color0; //lerp( prt.Color0, prt.Color1, Ramp( prt.FadeIn, prt.FadeOut, factor ) );
//	float3 position	=	prt.Position;// + float2(0, VelocityRecountY(prt.Size0)) * time;// + prt.Acceleration * time * time / 2;
	float4 position	=	float4(prt.Position, 1);// + float2(0, VelocityRecountY(prt.Size0)) * time;// + prt.Acceleration * time * time / 2;
//	float2 position	=	prt.Position + prt.Velocity * time + prt.Acceleration * time * time / 2;
//	float  a		=	lerp( prt.Angle0, prt.Angle1, factor );	

//	float2x2	m	=	float2x2( cos(a), sin(a), -sin(a), cos(a) );
//	float3x3	m	=	float3x3( 1, 1, 1, 1, 1, 1, 1, 1, 1 );
	float4 pp		=	mul(position, Params.View); 
	
//	p0.Position	= mul( float4( position + mul(float3( sz, sz, 0), m),  1 ), Params.Projection );
	p0.Position	= mul( pp + float4( sz, sz, 0, 0), Params.Projection );
	p0.TexCoord	= float2(1,1);
	p0.Color 	= color;
	
//	p1.Position	= mul( float4( position + mul(float3(-sz, sz, 0), m),  1 ), Params.Projection );
	p1.Position	= mul( pp + float4( -sz, sz, 0, 0), Params.Projection );
	p1.TexCoord	= float2(0,1);
	p1.Color 	= color;
	
//	p2.Position	= mul( float4( position + mul(float3(-sz,-sz, 0), m),  1 ), Params.Projection );
	p2.Position	= mul( pp + float4( -sz, -sz, 0, 0), Params.Projection );
	p2.TexCoord	= float2(0,0);
	p2.Color 	= color;
	
//	p3.Position	= mul( float4( position + mul(float3( sz,-sz, 0), m),  1 ), Params.Projection );
	p3.Position	= mul( pp + float4( sz, -sz, 0, 0), Params.Projection );
	p3.TexCoord	= float2(1,0);
	p3.Color 	= color;

	outputStream.Append(p0);
	outputStream.Append(p1);
	outputStream.Append(p2);
	
	outputStream.RestartStrip();

	outputStream.Append(p0);
	outputStream.Append(p2);
	outputStream.Append(p3);

	outputStream.RestartStrip();
}



float4 PSMain( GSOutput input ) : SV_Target
{
	return Texture.Sample( Sampler, input.TexCoord ) * float4(input.Color.rgb,1);
}
#endif

