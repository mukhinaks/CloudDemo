using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Mathematics;
using Fusion.Graphics;
using Fusion.Audio;
using Fusion.Input;
using Fusion.Content;
using Fusion.Development;
using Fusion.UserInterface;

namespace CloudDemo {
	public class CloudDemo: Game {
		/// <summary>
		/// CloudDemo constructor
		/// </summary>
		public CloudDemo ()
			: base()
		{
			//	enable object tracking :
			Parameters.TrackObjects = true;
			

			//	uncomment to enable debug graphics device:
			//	(MS Platform SDK must be installed)
			//	Parameters.UseDebugDevice	=	true;

			//	add services :
			AddService(new SpriteBatch(this), false, false, 0, 0);
			AddService(new DebugStrings(this), true, true, 9999, 9999);
			AddService(new DebugRender(this), true, true, 9998, 9998);
			AddService(new Camera(this), true, false, 1, 1);

			//	add here additional services :
			AddService( new CloudSystem( this ), true, true, 500, 500 );
			AddService( new CloudConfigService( this ), false, false, 1000, 1000 );

			//	load configuration for each service :
			LoadConfiguration();

			//	make configuration saved on exit :
			Exiting += Game_Exiting;
		}

		struct ConstData {
			public Matrix Projection;
			public Matrix View;
			public Matrix World;
			public Vector4 ViewPos;
			public Vector4 Color;
		}

		class Material {
			public Texture2D	Texture;
		}

		Scene				scene;
		VertexBuffer[]		vertexBuffers;
		IndexBuffer[]		indexBuffers;
		Texture2D[]			textures;
		Matrix[]			worldMatricies;
		Ubershader			ubershader;
		//CBData				constData;

		
		ConstantBuffer		constBuffer;

		Texture2D			texture256;

		ConstData			cbData;
		StateFactory		factory;


		Random	rand = new Random();
		
		
		enum UberFlags {
			None = 0,
			//NONE = 0,
			//USE_VERTEX_COLOR = 1,
			//USE_TEXTURE = 2,
		}

		/// <summary>
		/// Initializes game :
		/// </summary>
		protected override void Initialize ()
		{
			//	initialize services :
			base.Initialize();

			//	add keyboard handler :
			InputDevice.KeyDown += InputDevice_KeyDown;			

			texture256 = new Texture2D(GraphicsDevice, 256, 256, ColorFormat.Rgba8, false);
			

			//	load content & create graphics and audio resources here:
			LoadContent();

			constBuffer = new ConstantBuffer(GraphicsDevice, typeof(ConstData));

			

			Reloading += (s, e) => LoadContent();
		}

		void LoadContent ()
		{
			SafeDispose( ref factory );
			SafeDispose( ref indexBuffers );

			//texture		=	Content.Load<Texture2D>("lena.tga" );
			ubershader = Content.Load<Ubershader>("shader.hlsl");
			factory		=	new StateFactory( 
								ubershader, 
								typeof(UberFlags), 
								Primitive.TriangleList, 
								VertexColorTextureNormal.Elements,
								BlendState.Opaque,
								RasterizerState.CullCW,
								DepthStencilState.Default 
							);
			scene		=	Content.Load<Scene>(@"Scenes\testScene");
			
			vertexBuffers	=	scene.Meshes
							.Select( m => VertexBuffer.Create( GraphicsDevice, m.Vertices.Select( v => VertexColorTextureNormal.Convert(v) ).ToArray() ) )
							.ToArray();

			indexBuffers	=	scene.Meshes
							.Select( m => IndexBuffer.Create( GraphicsDevice, m.GetIndices() ) )
							.ToArray();

			textures		=	scene.Materials
							.Select( mtrl => Content.Load<Texture2D>( mtrl.TexturePath ) )
							.ToArray();

			worldMatricies	=	new Matrix[ scene.Nodes.Count ];
			scene.CopyAbsoluteTransformsTo( worldMatricies );
			

			//Perlin Noise
			//float factor =  rand.NextFloat(100);
			float factor =   rand.NextFloat((float) Math.PI * 2 * 10, (float) Math.PI * 2.5f * 10);
			//var data = Enumerable.Range(0, 256*256).Select((i)=> rand.NextColor()).ToArray();
			
			//var data = Enumerable.Range(0, 256 * 256).Select((i) => new Color(perlinNoise((float) i / 256, (float) i % 256, factor))).ToArray();
			//texture256.SetData(data);

			//creating matrix of noise
			int size = 512;
			int factorRnd =   rand.Next(2000);

			//double[][] floatMap = new double[size][];
			float[][] floatMap = new float[size][];
			for (int j = 0; j < size; j++) {
				//floatMap[j] = Enumerable.Range( 0, size ).Select( (i) =>  PerlinNoiseGenerator.Noise(i, j)  ).ToArray();
				floatMap[j] = Enumerable.Range( 0, size ).Select( (i) =>  perlinNoise( (float) (j * size + i) / size, (float) (i + size * j) % size, factor )  ).ToArray();
			}

			//clouds
			//var cs = GetService<CloudSystem>();
			//cs.AddParticle( Vector3.Zero, Vector3.Zero, 3, Color.White );//rand.NextFloat( 100, 900 ));


			

		}


		/// <summary>
		/// Disposes game
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose (bool disposing)
		{
			if ( disposing ) {
				//	dispose disposable stuff here
				//	Do NOT dispose objects loaded using ContentManager.
				
				SafeDispose(ref texture256);

				SafeDispose( ref constBuffer );
				SafeDispose( ref factory );
				SafeDispose( ref vertexBuffers );
				SafeDispose( ref indexBuffers );
				
			}
			base.Dispose(disposing);
		}



		/// <summary>
		/// Handle keys
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void InputDevice_KeyDown (object sender, Fusion.Input.InputDevice.KeyEventArgs e)
		{
			if ( e.Key == Keys.F1 ) {
				DevCon.Show(this);
			}

			if ( e.Key == Keys.F5 ) {
				Reload();
			}

			if ( e.Key == Keys.F12 ) {
				GraphicsDevice.Screenshot();
			}

			if ( e.Key == Keys.Escape ) {
				Exit();
			}
		}



		/// <summary>
		/// Saves configuration on exit.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Game_Exiting (object sender, EventArgs e)
		{
			SaveConfiguration();
		}



		/// <summary>
		/// Updates game
		/// </summary>
		/// <param name="gameTime"></param>
		protected override void Update (GameTime gameTime)
		{
			var ds	=	GetService<DebugStrings>();

			ds.Add(Color.Orange, "FPS {0}", gameTime.Fps);
			ds.Add("F1   - show developer console");
			ds.Add("F5   - build content and reload textures");
			ds.Add("F12  - make screenshot");
			ds.Add("ESC  - exit");


			base.Update(gameTime);

			//	Update stuff here :
			var cam	=	GetService<Camera>();
			var dr	=	GetService<DebugRender>();
			dr.View			=	cam.GetViewMatrix( StereoEye.Mono );
			dr.Projection	=	cam.GetProjectionMatrix( StereoEye.Mono );

			dr.DrawGrid(10);
		}


		/// <summary>
		/// Draws game
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="stereoEye"></param>
		protected override void Draw (GameTime gameTime, StereoEye stereoEye)
		{
			GraphicsDevice.ClearBackbuffer(Color.CornflowerBlue, 1, 0);
			var cam	=	GetService<Camera>();
			var sb = GetService<SpriteBatch>();
			
				
			//draw scene
			cbData.View = cam.GetViewMatrix( stereoEye );
			cbData.Projection = cam.GetProjectionMatrix( stereoEye );
			cbData.ViewPos = cam.GetCameraPosition4( stereoEye );
			cbData.World = Matrix.Identity;
			//constBuffer.SetData( cbData );


			GraphicsDevice.PipelineState = factory[0];

			GraphicsDevice.PixelShaderConstants[0] = constBuffer;
			GraphicsDevice.VertexShaderConstants[0] = constBuffer;
			GraphicsDevice.PixelShaderSamplers[0] = SamplerState.AnisotropicWrap;


			for (int j = 0; j < 40; j++) {

				GraphicsDevice.PipelineState = factory[0];
				GraphicsDevice.PixelShaderSamplers[0] = SamplerState.AnisotropicWrap;
				GraphicsDevice.PixelShaderConstants[0] = constBuffer;
				GraphicsDevice.VertexShaderConstants[0] = constBuffer;


				for (int i=0; i < scene.Nodes.Count; i++) {

					int meshId	=	scene.Nodes[i].MeshIndex;

					if (meshId < 0) {
						continue;
					}

					cbData.World = worldMatricies[i];
					constBuffer.SetData( cbData );

					GraphicsDevice.SetupVertexInput( vertexBuffers[meshId], indexBuffers[meshId] );

					foreach (var subset in scene.Meshes[meshId].Subsets) {
						GraphicsDevice.PixelShaderResources[0] = textures[subset.MaterialIndex];
						GraphicsDevice.DrawIndexed( subset.PrimitiveCount * 3, subset.StartPrimitive * 3, 0 );
					}
				}
			}

						
			base.Draw(gameTime, stereoEye);

			//	Draw stuff here :
		}

		//Perlin Noise
		float perlinRandom (int x, int y)
		{
			int n = x + y * 57;
			n = ( n << 13 ) ^ n;
			return ( 1.0f - ( ( n * ( n * n * 15731 + 789221 ) + 1376312589 ) & 0x7fffffff ) / 1073741824.0f );
		}

		float cosSmooth (float a, float b, float x)
		{
			double ft = x * 3.1415927;
			double f =  ( 1 - Math.Cos(ft) ) * 0.5f;
			float result = (float) ( a * ( 1 - f ) + b * f );
			return result;
		}

		float smoothNoise (float x, float y)
		{
			int xint = (int) x;
			int yint = (int) y;
			float corners = ( perlinRandom(xint - 1, yint - 1) + perlinRandom(xint + 1, yint - 1) + perlinRandom(xint - 1, yint + 1) + perlinRandom(xint + 1, yint + 1) ) / 16;
			float sides   = ( perlinRandom(xint - 1, yint) + perlinRandom(xint + 1, yint) + perlinRandom(xint, yint - 1) + perlinRandom(xint, yint + 1) ) / 8;
			float center  =  perlinRandom(xint, yint) / 4;
			return corners + sides + center;
		}

		float compileNoise (float x, float y)
		{
			float int_X    =  (float) Math.Floor(x);//целая часть х
			float fractional_X = x - int_X;//дробь от х
			//аналогично у
			float int_Y    = (float) Math.Floor(y);
			float fractional_Y = y - int_Y;
			//получаем 4 сглаженных значения
			float v1 = smoothNoise(int_X, int_Y);
			float v2 = smoothNoise(int_X + 1, int_Y);
			float v3 = smoothNoise(int_X, int_Y + 1);
			float v4 = smoothNoise(int_X + 1, int_Y + 1);
			//интерполируем значения 1 и 2 пары и производим интерполяцию между ними
			float i1 = cosSmooth(v1, v2, fractional_X);
			float i2 = cosSmooth(v3, v4, fractional_X);
			//я использовал косинусною интерполяцию ИМХО лучше 
			//по параметрам быстрота-//качество
			return cosSmooth(i1, i2, fractional_Y);
		}

		float perlinNoise (float x, float y, float factor)
		{
			float total = 0;
			// это число может иметь и другие значения хоть cosf(sqrtf(2))*3.14f 
			// главное чтобы было красиво и результат вас устраивал
			float persistence=0.5f;
			//float persistence=0.5f;

			// экспериментируйте с этими значениями, попробуйте ставить 
			// например sqrtf(3.14f)*0.25f или что-то потяжелее для понимания J)
			//float frequency = 0.25f;
			float frequency = 0.15f;
			float amplitude=1;//амплитуда, в прямой зависимости от значения настойчивости

			// вводим фактор случайности, чтобы облака не были всегда одинаковыми
			// (Мы ведь помним что ф-ция шума когерентна?) 

			x += ( factor );
			y += ( factor );

			// NUM_OCTAVES - переменная, которая обозначает число октав,
			// чем больше октав, тем лучше получается шум
			for ( int i=0; i < 4; i++ ) {
				total += compileNoise(x * frequency, y * frequency) * amplitude;
				amplitude *= persistence;
				frequency *= 2;
			}
			//здесь можно перевести значения цвета   по какой-то формуле
			//например:
			total=(float) Math.Sqrt(total);
			// total=total*total;
			// total=sqrt(1.0f/float(total)); 
			//total=255-total;-и.т.д все зависит от желаемого результата
			//total=fabsf(total);
			float res = total; //*255.0f;//приводим цвет к значению 0-255…
			return res;
		}
	}
}
