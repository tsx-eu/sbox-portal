using System;
using System.Collections.Generic;
using Sandbox;

namespace PortalGame
{
	
	[Library( "portal_dropper" )]
	[Hammer.Model( Model = "models/props_backstage/item_dropper.vmdl" )]
	public partial class Dropper : AnimEntity
	{
		private List<Cube> listOfDroppedCube = new List<Cube>();

		[Property( "init_amount", Title = "Initial amount of cube to drop" )]
		public int AmountOfCubeToDrop { get; set; } = 1;
		public float delayBetweenSpawn { get; set; } = 0.1f;

		private TimeSince lastSpawn = 0;
		private DoorEntity t;

		public override void Spawn() {
			base.Spawn();
			SetModel( "models/props_backstage/item_dropper.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
			SetAnimGraph( "animgraphs/item_dropper.vanmgrph" );
			ResetAnimParameters();
			lastSpawn = 0;
		}


		[Input]
		public void Open() {
			SetAnimParameter( "enable", true );
		}

		[Input]
		public void Close() {
			SetAnimParameter( "enable", false );
		}

		[Input]
		public void SetCube(int cubeCount) {
			AmountOfCubeToDrop = cubeCount;
		}


		[Event.Tick.Server]
		public void OnServerTick() {
			if( AmountOfCubeToDrop > listOfDroppedCube.Count && lastSpawn > delayBetweenSpawn ) {
				lastSpawn = 0;

				var cube = new Cube {
					Position = Position + Vector3.Up * 64,
					Rotation = Rotation.Random
				};
				cube.Spawn();

				listOfDroppedCube.Add( cube );
			}


			// TODO cube should trigger an "on destroy" / "on kill"
			for(int i=0; i< listOfDroppedCube.Count; i++) {
				var cube = listOfDroppedCube[i];
				if ( !cube.IsValid() ) {
					listOfDroppedCube.RemoveAt( i );
					i--;
				}
			}
		}

	}
}
