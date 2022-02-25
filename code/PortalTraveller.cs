
namespace Sandbox
{
	public partial class PortalTraveller : EntityComponent
	{
		public Vector3 previousOffsetFromPortal { get; set; }

		public virtual void Teleport(Portal from, Portal to) {
			var pos = from.GetPosition( Entity );
			var rot = from.GetRotation( Entity );

			Entity.Position = pos;

			if ( Entity is PortalPlayer player )
				player.Teleport( pos, rot );
			else
				Entity.Rotation = rot;
		}

		public virtual void EnterPortalThreshold(Portal portal) {

		}
		public virtual void ExitPortalThreshold(Portal portal) {

		}

	}
}
