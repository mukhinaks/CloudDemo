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
		Texture2D		noise;
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
			ConstantBuffer constBuffer;
		VertexBuffer vb;
		List<Cloud> list;
		int numberOfPoints;
		float step;
 		float radius;

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
		}



//       row_major float4x4 View;       // Offset:    0
//       row_major float4x4 Projection; // Offset:   64
//       int MaxParticles;              // Offset:  128
//       float DeltaTime;               // Offset:  132
	struct CBData {
			public Fusion.Mathematics.Matrix Projection;
			public Fusion.Mathematics.Matrix View;
			public Fusion.Mathematics.Matrix World;
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

			constBuffer = new ConstantBuffer(Game.GraphicsDevice, typeof(CBData));
			vb = new VertexBuffer( Game.GraphicsDevice, typeof( Cloud ), MaxSimulatedParticles ); 
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
			noise		=	Game.Content.Load<Texture2D>("noise2");
			shader		=	Game.Content.Load<Ubershader>("test");
			//int index = 128;
			//float[][] floatMap = new float[index][];
			//for (int j = 0; j < size; j++) {
			//	floatMap[j] = Enumerable.Range( 0, index ).Select( (i) => (float) PerlinNoiseGenerator.Noise(i, j)  ).ToArray();
			//}
			var cc = Game.GetService<CloudConfigService>();
			step = cc.Config.Step;
			//radius
			radius = cc.Config.Radius;
			numberOfPoints = cc.Config.NumberOfPoints;
			float size	= cc.Config.Size;

			//add grid
			Vector3 start = new Vector3( -numberOfPoints * step / 2, 0, - numberOfPoints * step / 2 );
			Vector3 position = start;
			for (int i = 0; i < numberOfPoints; i++) {
				for (int j = 0; j < numberOfPoints; j++) {
					position = start +  new Vector3( step * i, 50, step *  j );
					
					float z2	= position.X * position.X + position.Z * position.Z;	//z^2 vertical coord
					float r4	= 4 * radius * radius;		//4r
					//3D coordinates from 2D (Stereographic projection)
					float ksi = position.X *  r4 / (r4 + z2);
					float dzeta = - z2 * radius * 2 / (r4 + z2) + radius;//+ radius / 2 ; 
					float eta = position.Z * r4 / (r4 + z2) ;
					//var s = size;
					AddPoint( new Vector3( ksi, dzeta / 2, eta ), Vector3.Up, Color.White.ToVector4(), Vector2.Zero, size );
					//ps.AddParticle( position, Vector2.Zero, 9999, s, s );
					//Log.Message("{0}  {1}", s, position );
				}
			}
			factory		=	new StateFactory( shader, typeof(RenderFlags), (ps,i) => StateEnum( ps, (RenderFlags)i) );
			//numberOfPoints = list.Count;
			vb.SetData( list.ToArray(), 0, numberOfPoints * numberOfPoints );
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
				constBuffer.Dispose();
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
			CBData cbData = new CBData();

			var cam = Game.GetService<Camera>();

			cbData.Projection = cam.GetProjectionMatrix(stereoEye);
			cbData.View = cam.GetViewMatrix(stereoEye);
			cbData.World = Matrix.Identity;
			cbData.CameraPos = new Vector4(cam.FreeCamPosition.X, 0, cam.FreeCamPosition.Z, 0);
//			cbData.ViewPos = new Vector4( cam.GetCameraMatrix( stereoEye ).TranslationVector, 1 );


			constBuffer.SetData(cbData);
			Game.GraphicsDevice.PipelineState = factory[0];
			
		//	Game.GraphicsDevice.SetTargets(null, rt.Surface);

			Game.GraphicsDevice.PixelShaderConstants[0] = constBuffer;
			Game.GraphicsDevice.VertexShaderConstants[0] = constBuffer;
			Game.GraphicsDevice.VertexShaderSamplers[0] = SamplerState.LinearWrap;
			Game.GraphicsDevice.VertexShaderResources[1] = noise;
			Game.GraphicsDevice.GeometryShaderConstants[0] = constBuffer;
			Game.GraphicsDevice.PixelShaderSamplers[0] = SamplerState.LinearWrap;
			Game.GraphicsDevice.PixelShaderResources[0] = texture;

			// setup data and draw points
			Game.GraphicsDevice.SetupVertexInput( vb, null );
			Game.GraphicsDevice.Draw(numberOfPoints * numberOfPoints, 0);


			base.Draw( gameTime, stereoEye );
		}
	}
}
