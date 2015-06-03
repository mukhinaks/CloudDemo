using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
		//Cloud[]				injectionBufferCPU = new Cloud[MaxInjectingParticles];
		//StructuredBuffer	injectionBuffer;
		//StructuredBuffer	simulationBufferSrc;
		//StructuredBuffer	simulationBufferDst;
		ConstantBuffer		paramsCB;
		VertexBuffer vb;
		List<Cloud> list;
		int numberOfPoints = 0;

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
			 [Vertex("POSITION")]
			public Vector3 Position;
			[Vertex("NORMAL")]
			public Vector3 Normal;
			[Vertex("COLOR")]
			public Vector4 Color;
			[Vertex("TEXCOORD", 0)]
			public Vector2 TexCoord;
			[Vertex("TEXCOORD", 1)]
			public float Size;
			[Vertex("TEXCOORD", 2)]
			public float Angle;
			

			
		}

		enum RenderFlags {
			None,
			RELATIVE	= 0x1,
			FIXED		= 0x2,
		}



//       row_major float4x4 View;       // Offset:    0
//       row_major float4x4 Projection; // Offset:   64
//       int MaxParticles;              // Offset:  128
//       float DeltaTime;               // Offset:  132

		struct Params {
			 public Matrix	View;
			 public Matrix	Projection;
			public Matrix World;
			public Vector4 CameraPos;

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
			vb = new VertexBuffer( Game.GraphicsDevice, typeof( Cloud ), 128*128 ); 
			list = new List<Cloud>();
						
			base.Initialize();
			Game.Reloading += Game_Reloading;
			Game_Reloading(this, EventArgs.Empty);
			
		}

		void Game_Reloading ( object sender, EventArgs e )
		{
			SafeDispose( ref factory );
			list.Clear();
			texture		=	Game.Content.Load<Texture2D>(@"Scenes\cloud1");
			shader		=	Game.Content.Load<Ubershader>("test");
			float size	= 10.0f;

			//clouds from noise
			for (int i = 0; i < size; i++ ) {
				for (int j = 0; j < size; j++) {
					if (floatMap[i][j] > 0.3) {

						//radius
						int r = 200;

						//2D coordinates of noise
						float x		= i - size / 2;
						float y		= j - size / 2;
						float z2	= x * x + y * y;	//z^2 vertical coord
						float r4	= 4 * r * r;		//4r

						//3D coordinates from 2D (Stereographic projection)
						float ksi = x *  r4 / (r4 + z2);
						float dzeta = - z2 * r * 2 / (r4 + z2) + r / 2; 
						float eta = y * r4 / (r4 + z2) ;
			
						//on the sphere
						AddPoint( new Vector3( ksi, dzeta, eta ), Vector3.Zero,  new Color(floatMap[i][j]), Vector2.Zero, size  );
			
						//on the plane
						//cs.AddParticle( new Vector3( i - size / 2, 0, j - size/2 ), Vector3.Zero, 1, new Color((float) floatMap[i][j]) );
					}
				}
			}

			factory		=	new StateFactory( shader, typeof(RenderFlags), (ps,i) => StateEnum( ps, (RenderFlags)i) );
			numberOfPoints = list.Count;
			vb.SetData( list.ToArray(), 0, numberOfPoints );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="ps"></param>
		/// <param name="flags"></param>
		void StateEnum ( PipelineState ps, RenderFlags flags )
		{
			ps.BlendState			=	BlendState.Screen;
			ps.DepthStencilState	=	DepthStencilState.Readonly;
			ps.Primitive			=	Primitive.PointList;
			ps.VertexInputElements = VertexInputElement.FromStructure<Cloud>();

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
		public void AddPoint(Vector3 pos, Vector3 normal, Vector4 color, Vector2 texcoord, float size) {
			

			var p = new Cloud() {
				Position	= pos,
				Normal		= normal, 
				Color		= color, //Color.White.ToVector4(),//
				TexCoord	= texcoord,
				Size		= size,
				Angle		= rand.NextFloat( -MathUtil.Pi, MathUtil.Pi ),
			};

			list.Add( p );
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
				SafeDispose(ref vb);
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
			param.CameraPos		= new Vector4(cam.FreeCamPosition.X, 0, cam.FreeCamPosition.Z, 0);;
			param.World			= Matrix.Identity;

			paramsCB.SetData(param);
			device.PipelineState = factory[(int) ( RenderFlags.FIXED)];
			
			device.ComputeShaderConstants[0]	= paramsCB ;
			device.VertexShaderConstants[0]	= paramsCB ;
			device.GeometryShaderConstants[0]	= paramsCB ;
			device.PixelShaderConstants[0]	= paramsCB ;
			
			device.PixelShaderSamplers[0]	= SamplerState.LinearWrap;
			device.PixelShaderResources[0] = texture;

			// setup data and draw points
			device.SetupVertexInput( vb, null );
			device.Draw(numberOfPoints, 0);

			base.Draw( gameTime, stereoEye );
		}
	}
}
