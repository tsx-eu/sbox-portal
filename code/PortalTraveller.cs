
namespace Sandbox
{
	public partial class PortalTraveller : EntityComponent
	{
		public Vector3 previousOffsetFromPortal { get; set; }

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

		}
		public void ExitPortalThreshold(Portal portal) {

		}

	}
}
