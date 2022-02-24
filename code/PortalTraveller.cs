
namespace Sandbox
{
	public partial class PortalTraveller : EntityComponent
	{
		public Vector3 previousOffsetFromPortal { get; set; }

		public virtual void Teleport(Portal from, Portal to) {
			Entity.Position = to.Position;
			Entity.Rotation = to.Rotation;
		}

		public virtual void EnterPortalThreshold(Portal portal) {

		}
		public virtual void ExitPortalThreshold( Portal portal) {

		}

	}
}
