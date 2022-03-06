using System;
using Sandbox;

namespace PortalGame
{
	public partial class PortalTraveller : EntityComponent
	{
		public Vector3 previousOffsetFromPortal { get; set; }


		[Net] public AnimEntity clone { get; set; }
		public Portal entrance;

		public void Teleport(Vector3 pos, Rotation rot) {
			Rotation deltaRotation;
			Entity.Position = pos;

			if ( Entity is PortalPlayer player ) {
				deltaRotation = player.EyeRotation.Inverse * rot;
				player.SetRotation( rot );
			}
			else {
				deltaRotation = Entity.Rotation.Inverse * rot;
				Entity.Rotation = rot;
			}

			Entity.Velocity *= deltaRotation;
		}

		public void EnterPortalThreshold(Portal portal) {
			entrance = portal;

			return;
			if ( clone == null && Host.IsServer ) {
				Player origin = Entity as Player;

				clone = new AnimEntity();
				clone.Model = origin.Model;
				clone.PhysicsEnabled = origin.PhysicsEnabled;
				//clone.UseAnimGraph = origin.UseAnimGraph;
				//clone.SetAnimGraph( "models/citizen/citizen.vanmgrph" );
				clone.Spawn();
			}

		}
		public void ExitPortalThreshold( Portal portal )
		{
			if ( clone != null ) {
				clone.Delete();
				clone = null;
			}
		}


		[Event.Tick.Server]
		public void OnRender()
		{
			if ( clone != null ) {
				CopyParams( (Entity as PortalPlayer).Animator as PortalAnimator, clone );
				clone.Position = entrance.GetPosition( Entity );
//				clone.Rotation = entrance.GetRotation( Entity );
			}
		}

		private void CopyParams( PortalAnimator from, AnimEntity to )
		{
			try
			{

				foreach ( var animParam in from.Params )
				{
					if ( animParam.Value is bool boolAnimValue )
						to.SetAnimParameter( animParam.Key, boolAnimValue );
					if ( animParam.Value is int intAnimValue )
						to.SetAnimParameter( animParam.Key, intAnimValue );
					//if ( animParam.Value is float floatAnimValue )
					//	to.SetAnimParameter( animParam.Key, floatAnimValue );
					if ( animParam.Value is Vector3 vector3AnimValue )
						to.SetAnimParameter( animParam.Key, vector3AnimValue );

					if ( animParam.Value is string strAnimValue )
						to.SetProperty( animParam.Key, strAnimValue );
				}
			}
			catch ( Exception e ) {
				Log.Error( e );
			}
		}

	}
}
