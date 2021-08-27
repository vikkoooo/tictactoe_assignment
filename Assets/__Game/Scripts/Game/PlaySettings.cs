using UnityEngine;

namespace FG {
	public class PlaySettings : MonoBehaviour {
		public static int BoardSize { get; set; } = 5;
		public static int SlotsToWin { get; set; } = 3;
		public static string PlayerOneName { get; set; } = "Player one";
		public static string PlayerTwoName { get; set; } = "Player two";
	}
}
