using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion;
using Fusion.Mathematics;
using Fusion.Graphics;

namespace CloudDemo {
	class CloudSystem: GameService {
		Texture2D		texture;
		Ubershader		shader;
		StateFactory	factory;

		const int BlockSize				=	512; 
		//const int MaxInjectingParticles	=	256 * 256;
		//const int MaxSimulatedParticles =	256 * 256;
		const int MaxInjectingParticles	=	1024 * 1024;
		const int MaxSimulatedParticles =	1024 * 1024;

		int					injectionCount = 0;
		Cloud[]				injectionBufferCPU = new Cloud[MaxInjectingParticles];
		StructuredBuffer	injectionBuffer;
		StructuredBuffer	simulationBufferSrc;
		StructuredBuffer	simulationBufferDst;
		ConstantBuffer		paramsCB;

//       float2 Position;               // Offset:    0
//       float2 Velocity;               // Offset:    8
//       float2 Acceleration;           // Offset:   16
//       float4 Color0;                 // Offset:   24
//       float4 Color1;                 // Offset:   40
//       float Size0;                   // Offset:   56
//       float Size1;                   // Offset:   60
//       float Angle0;                  // Offset:   64
//       float Angle1;                  // Offset:   68
//       float TotalLifeTime;           // Offset:   72
//       float LifeTime;                // Offset:   76
//       float FadeIn;                  // Offset:   80
//       float FadeOut;                 // Offset:   84
		
		struct Cloud {
			 public Vector3	Position;
			 public Vector3	Velocity;
			// public Vector3	Acceleration;
			 public Vector4	Color0;
			 public float	Size0;
			

			
		}

		enum Flags {
			INJECTION	=	0x1,
			SIMULATION	=	0x2,
			DRAW		=	0x4,
		}


//       row_major float4x4 View;       // Offset:    0
//       row_major float4x4 Projection; // Offset:   64
//       int MaxParticles;              // Offset:  128
//       float DeltaTime;               // Offset:  132

		struct Params {
			 public Matrix	View;
			 public Matrix	Projection;
			 public int		MaxParticles;
			 public float	DeltaTime;
			 public Vector3 Position;
			 public float	RadiusMin;
			 public float	RadiusMax;
			 public float	NumberofCircles;

		} 

		Random rand = new Random();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public CloudSystem ( Game game ) : base (game)
		{
		}


		/// <summary>
		/// 
		/// </summary>
		public override void Initialize ()
		{

			paramsCB			=	new ConstantBuffer( Game.GraphicsDevice, typeof(Params) );

			injectionBuffer		=	new StructuredBuffer( Game.GraphicsDevice, typeof(Cloud), MaxInjectingParticles, StructuredBufferFlags.None );
			simulationBufferSrc	=	new StructuredBuffer( Game.GraphicsDevice, typeof(Cloud), MaxSimulatedParticles, StructuredBufferFlags.Append );
			simulationBufferDst	=	new StructuredBuffer( Game.GraphicsDevice, typeof(Cloud), MaxSimulatedParticles, StructuredBufferFlags.Append );

			base.Initialize();
			Game.Reloading += Game_Reloading;
			Game_Reloading(this, EventArgs.Empty);
			
		}

		void Game_Reloading ( object sender, EventArgs e )
		{
			SafeDispose( ref factory );

			texture		=	Game.Content.Load<Texture2D>(@"Scenes\cloud1");
			shader		=	Game.Content.Load<Ubershader>("test");


			factory		=	new StateFactory( shader, typeof(Flags), (ps,i) => StateEnum( ps, (Flags)i) );
			
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="ps"></param>
		/// <param name="flags"></param>
		void StateEnum ( PipelineState ps, Flags flags )
		{
			ps.BlendState			=	BlendState.Additive;
			ps.DepthStencilState	=	DepthStencilState.Readonly;
			ps.Primitive			=	Primitive.PointList;
		}



		/// <summary>
		/// Returns random radial vector
		/// </summary>
		/// <returns></returns>
		Vector2 RadialRandomVector ()
		{
			Vector2 r;
			do {
				r	=	rand.NextVector2( -Vector2.One, Vector2.One );
			} while ( r.Length() > 1 );

			//r.Normalize();

			return r;
		}



		/// <summary>
		/// Adds random particle at specified position
		/// </summary>
		/// <param name="p"></param>
		public void AddParticle ( Vector3 pos, Vector3 vel, float size0, Color color)
		{
			if (injectionCount>=MaxInjectingParticles) {
				Log.Warning("Too much injected particles per frame");
				return;
			}

			//Log.LogMessage("...particle added");
			//var v = vel + RadialRandomVector() * 5;
			//var a = rand.NextFloat( -MathUtil.Pi, MathUtil.Pi );
			//var s = (rand.NextFloat(0,1)>0.5f) ? -1 : 1;

			var p = new Cloud () {
				Position		=	pos,
				Velocity		=	Vector3.Zero, //vel + RadialRandomVector() * 5,
				//Acceleration	=	Vector3.Zero, //new Vector2 (0, 9.8f), // - v * 0.2f,
				Color0			=	color.ToVector4(),
				Size0			=	size0,
//				Color0			=	new Color( 236, 11, 67 ).ToVector4(),
//				Color0			=	new Color (25, 70, 186).ToVector4(),
				//Color1			=	rand.NextVector4( Vector4.Zero, Vector4.One ) * colorBoost,
			
			};

			injectionBufferCPU[ injectionCount ] = p;
			injectionCount ++;
		}



		/// <summary>
		/// Makes all particles wittingly dead
		/// </summary>
		void ClearParticleBuffer ()
		{
			for (int i=0; i<MaxInjectingParticles; i++) {
				//injectionBufferCPU[i].TotalLifeTime = -999999;
			}
			injectionCount = 0;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref factory );

				paramsCB.Dispose();

				injectionBuffer.Dispose();
				simulationBufferSrc.Dispose();
				simulationBufferDst.Dispose();
			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update ( GameTime gameTime )
		{
			base.Update( gameTime );

			var ds = Game.GetService<DebugStrings>();

			//ds.Add( Color.Yellow, "Total particles DST: {0}", simulationBufferDst.GetStructureCount() );
			//ds.Add( Color.Yellow, "Total particles SRC: {0}", simulationBufferSrc.GetStructureCount() );
		}




		/// <summary>
		/// 
		/// </summary>
		void SwapParticleBuffers ()
		{
			var temp = simulationBufferDst;
			simulationBufferDst = simulationBufferSrc;
			simulationBufferSrc = temp;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="stereoEye"></param>
		public override void Draw ( GameTime gameTime, Fusion.Graphics.StereoEye stereoEye )
		{
			var cam	=	Game.GetService<Camera>();
			
			var device	=	Game.GraphicsDevice;

			int	w	=	device.DisplayBounds.Width;
			int h	=	device.DisplayBounds.Height;

			Params param = new Params();
			param.View			= cam.GetViewMatrix( stereoEye );
			param.Projection	= cam.GetProjectionMatrix( stereoEye );
			//param.View			=	Matrix.Identity;
			//param.Projection	=	Matrix.OrthoOffCenterRH(0, w, h, 0, -9999, 9999);
			param.MaxParticles	=	0;
			param.DeltaTime		=	gameTime.ElapsedSec;
			param.Position		=	cam.FreeCamPosition;
			param.RadiusMax		=	30;
			param.RadiusMin		=	10;
			param.NumberofCircles = 3;


			device.ComputeShaderConstants[0]	= paramsCB ;
			device.VertexShaderConstants[0]	= paramsCB ;
			device.GeometryShaderConstants[0]	= paramsCB ;
			device.PixelShaderConstants[0]	= paramsCB ;
			
			device.PixelShaderSamplers[0]	= SamplerState.LinearWrap;
			

			//
			//	Inject :
			//
			injectionBuffer.SetData( injectionBufferCPU );
			device.Clear( simulationBufferDst, Int4.Zero );

			device.ComputeShaderResources[1]	= injectionBuffer ;
			device.SetCSRWBuffer( 0, simulationBufferDst, 0 );

			param.MaxParticles	=	injectionCount;
			paramsCB.SetData( param );
			//device.CSConstantBuffers[0] = paramsCB ;

			device.PipelineState	=	factory[ (int)Flags.INJECTION ];
			device.Dispatch( MathUtil.IntDivUp( MaxInjectingParticles, BlockSize ) );

			ClearParticleBuffer();

			//
			//	Simulate :
			//
			device.ComputeShaderResources[1]	= simulationBufferSrc ;

			param.MaxParticles	=	MaxSimulatedParticles;
			paramsCB.SetData( param );
			device.ComputeShaderConstants[0] = paramsCB ;

			device.PipelineState	=	factory[ (int)Flags.SIMULATION ];
			device.Dispatch( MathUtil.IntDivUp( MaxSimulatedParticles, BlockSize ) );//*/

			SwapParticleBuffers();


			//
			//	Render
			//
			device.PipelineState	=	factory[ (int)Flags.DRAW ];
			device.SetCSRWBuffer( 0, null );	
			device.PixelShaderResources[0]	=	texture ;
			device.GeometryShaderResources[1]	=	simulationBufferSrc ;

			device.Draw( MaxSimulatedParticles, 0 );


			/*var testSrc = new Particle[MaxSimulatedParticles];
			var testDst = new Particle[MaxSimulatedParticles];

			simulationBufferSrc.GetData( testSrc );
			simulationBufferDst.GetData( testDst );*/

			base.Draw( gameTime, stereoEye );
		}
	}
}
