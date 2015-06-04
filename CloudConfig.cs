using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace CloudDemo {
	public class CloudConfigService: GameService {
		
		[Config]
		public CloudConfig	Config { get; set; }

		public class CloudConfig{
		
			[Category( "Grid" )]
			public int NumberOfLayers { get; set; }

			[Category( "Grid" )]
			public int NumberOfPoints { get; set; }

			[Category( "Grid" )]
			public float Step { get; set; }

			[Category( "Grid" )]
			public float Radius { get; set; }

			[Category( "Grid" )]
			public float Size { get; set; }


			public CloudConfig(){
				NumberOfPoints		= 128;
				Step				= 4;
				Radius				= 200;
				Size				= 20;
				NumberOfLayers		= 2;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public CloudConfigService ( Game game ) : base(game)
		{
			Config	=	new CloudConfig();
		}
	}
}
