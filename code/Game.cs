using Sandbox;

namespace PortalGame
{
	public partial class Gamemode : Sandbox.Game
	{
		public Gamemode() {
			if( IsServer )
				new HUD();
		}

		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );

			var player = new PortalPlayer();
			client.Pawn = player;

			player.Respawn();
		}
	}
}
