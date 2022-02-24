
using Sandbox;
using System;
using System.Linq;

namespace Sandbox
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

			var player = new Pawn();
			client.Pawn = player;

			player.Respawn();
		}
	}
}
