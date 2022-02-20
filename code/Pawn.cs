using Sandbox;

namespace Sandbox
{
	partial class Pawn : Player
	{
		/// <summary>
		/// Called when the entity is first created 
		/// </summary>
		public override void Spawn()
		{
			base.Spawn();

			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;

			Controller = new WalkController();
			Camera = new GameCamera();
		}

		/// <summary>
		/// Called every tick, clientside and serverside.
		/// </summary>
		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

		}

		/// <summary>
		/// Called every frame on the client
		/// </summary>
		public override void FrameSimulate( Client cl )
		{
			base.FrameSimulate( cl );
		}
	}
}
