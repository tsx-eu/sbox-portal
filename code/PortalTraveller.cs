
namespace Sandbox
{
	public partial class PortalTraveller : EntityComponent
	{
		public Vector3 previousOffsetFromPortal { get; set; }

		public void Teleport(Vector3 pos, Rotation rot) {
			Entity.Position = pos;

			if ( Entity is PortalPlayer player )
				player.SetRotation( rot );
			else
				Entity.Rotation = rot;
		}

		public void EnterPortalThreshold(Portal portal) {

		}
		public void ExitPortalThreshold(Portal portal) {

		}

	}
}
