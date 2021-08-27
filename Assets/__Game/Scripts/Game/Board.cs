using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FG
{
	public class Board : MonoBehaviour
	{
		[SerializeField] private GameObject _tilePrefab;
		public Player playerOne;
		public Player playerTwo;

		[Header("Events")] public PlayerEvent switchPlayerEvent;

		public UnityEvent didPlaceEvent;

		private int _boardSize;
		private Tile[,] _tiles;
		private GamePiece[,] _pieces;
		private bool _hasWon; // To prevent game from continuing after winner already found

		private Transform _tilesTransform;
		private Transform _piecesTransform;

		private const float _timeBetweenMarkingWinningTiles = 0.5f;
		private const float _timeToFadeWinningTiles = 0.5f;
		public Player CurrentPlayer { get; private set; }

		public Tile this[int row, int column] => _tiles[row, column];

		public void Awake()
		{
			_tilesTransform = transform.GetChild(0);
			_piecesTransform = transform.GetChild(1);
			_boardSize = PlaySettings.BoardSize;
			_hasWon = false; // Initialize at startup

			_tiles = new Tile[_boardSize, _boardSize];
			_pieces = new GamePiece[_boardSize, _boardSize];

			SetupTiles();

			playerOne.displayName = PlaySettings.PlayerOneName;
			playerTwo.displayName = PlaySettings.PlayerTwoName;

			SetCurrentPlayer();
		}

		public bool PlaceMarkerOnTile(Tile tile)
		{
			// To prevent the game from continuing after winner already found
			if (_hasWon == true)
			{
				return false;
			}

			if (ReferenceEquals(CurrentPlayer, null))
			{
				return false;
			}

			if (ReferenceEquals(_pieces[tile.gridPosition.x, tile.gridPosition.y], null))
			{
				GamePiece piece = Instantiate(CurrentPlayer.piecePrefab,
					new Vector3(tile.gridPosition.x, -tile.gridPosition.y),
					Quaternion.identity, _piecesTransform)?.GetComponent<GamePiece>();
				if (!ReferenceEquals(piece, null))
				{
					piece.Owner = CurrentPlayer;
					_pieces[tile.gridPosition.x, tile.gridPosition.y] = piece;
				}

				didPlaceEvent.Invoke();

				// Call check win
				_hasWon = CheckWin(tile);
				if (_hasWon == true)
				{
					Debug.Log(CurrentPlayer + " has won! Exiting program..");
					return false;
				}

				SwitchPlayer();
				return true;
			}

			return false;
		}

		private IEnumerator MarkWinningTiles(List<Vector2Int> winningTiles, Color color)
		{
			foreach (Vector2Int tile in winningTiles)
			{
				StartCoroutine(FadeTile(_tiles[tile.x, tile.y], color));
				yield return new WaitForSeconds(_timeBetweenMarkingWinningTiles);
			}
		}

		private IEnumerator FadeTile(Tile tile, Color targetColor)
		{
			SpriteRenderer tileRenderer = tile.GetComponent<SpriteRenderer>();
			float elapsedTime = 0f;
			Color startColor = tileRenderer.color;
			float fadeTime = _timeToFadeWinningTiles;

			while (elapsedTime < fadeTime)
			{
				elapsedTime += Time.deltaTime;
				float blend = Mathf.Clamp01(elapsedTime / fadeTime);
				tileRenderer.color = Color.Lerp(startColor, targetColor, blend);
				yield return null;
			}

			tileRenderer.color = targetColor;
		}

		private void SwitchPlayer()
		{
			CurrentPlayer = ReferenceEquals(CurrentPlayer, playerOne) ? playerTwo : playerOne;
			switchPlayerEvent.Invoke(CurrentPlayer);
		}

		private void SetupTiles()
		{
			for (int x = 0; x < _boardSize; x++)
			{
				for (int y = 0; y < _boardSize; y++)
				{
					GameObject tileGo = Instantiate(_tilePrefab, new Vector3(x, -y, 0f), Quaternion.identity,
						_tilesTransform);
					tileGo.name = $"Tile_({x},{y})";

					Tile tile = tileGo.GetComponent<Tile>();
					tile.board = this;
					tile.gridPosition = new Vector2Int(x, y);

					_tiles[x, y] = tile;
				}
			}
		}

		private void SetCurrentPlayer()
		{
			CurrentPlayer = Random.Range(0, 2) == 0 ? playerOne : playerTwo;
			switchPlayerEvent.Invoke(CurrentPlayer);
		}

		/*
		 * Calls all the private check functions. Will check until winning condition is met.
		 * Checks like the clock, north, then north east, then east, then south east, and so on.
		 * It remembers choices for each individual method call of this method.
		 * Param: tile that user clicked
		 * Returns: true if winner, otherwise false
		 */
		public bool CheckWin(Tile tile)
		{
			// Get positions of the clicked tile
			int x = tile.gridPosition.x;
			int y = tile.gridPosition.y;

			// North
			bool north = CheckNorth(x, y);
			if (north == true) // If true we need to check again, else continue with next direction
			{
				// To make the game scalable, we need to check how many in row is needed to win
				int n = PlaySettings.SlotsToWin - 2; // This gives us number of loops to perform
				int alreadyInRow = 2; // How many already in a row? Will always be two, or no point of us being here

				for (int i = 0; i < n; i++)
				{
					y--; // Move "up"

					bool nextNorth = CheckNorth(x, y); // Check the new coordinates

					if (nextNorth == true) // One more in a row found
					{
						alreadyInRow++; // Increase to check if we have reached the winning number
						if (alreadyInRow == PlaySettings.SlotsToWin)
						{
							return true; // Winner
						}
					}
				}
			}

			// North east
			bool northEast = CheckNorthEast(x, y);
			if (northEast == true)
			{
				int n = PlaySettings.SlotsToWin - 2;
				int alreadyInRow = 2;

				for (int i = 0; i < n; i++)
				{
					y--; // Move "up"
					x++; // Move "right"

					bool nextNorthEast = CheckNorthEast(x, y);

					if (nextNorthEast == true)
					{
						alreadyInRow++;
						if (alreadyInRow == PlaySettings.SlotsToWin)
						{
							return true; // Winner
						}
					}
				}
			}

			// East
			bool east = CheckEast(x, y);
			if (east == true)
			{
				int n = PlaySettings.SlotsToWin - 2;
				int alreadyInRow = 2;

				for (int i = 0; i < n; i++)
				{
					x++; // Move "right"

					bool nextEast = CheckEast(x, y);

					if (nextEast == true)
					{
						alreadyInRow++;
						if (alreadyInRow == PlaySettings.SlotsToWin)
						{
							return true; // Winner
						}
					}
				}
			}

			// South east
			bool southEast = CheckSouthEast(x, y);
			if (southEast == true)
			{
				int n = PlaySettings.SlotsToWin - 2;
				int alreadyInRow = 2;

				for (int i = 0; i < n; i++)
				{
					y++; // Move "down"
					x++; // Move "right"

					bool nextSouthEast = CheckSouthEast(x, y);

					if (nextSouthEast == true)
					{
						alreadyInRow++;
						if (alreadyInRow == PlaySettings.SlotsToWin)
						{
							return true; // Winner
						}
					}
				}
			}

			// South
			bool south = CheckSouth(x, y);
			if (south == true)
			{
				int n = PlaySettings.SlotsToWin - 2;
				int alreadyInRow = 2;

				for (int i = 0; i < n; i++)
				{
					y++; // Move "down"

					bool nextSouth = CheckSouth(x, y);

					if (nextSouth == true)
					{
						alreadyInRow++;
						if (alreadyInRow == PlaySettings.SlotsToWin)
						{
							return true; // Winner
						}
					}
				}
			}

			// South west
			bool southWest = CheckSouthWest(x, y);
			if (southWest == true)
			{
				int n = PlaySettings.SlotsToWin - 2;
				int alreadyInRow = 2;

				for (int i = 0; i < n; i++)
				{
					y++; // Move "down"
					x--; // Move "left"

					bool nextSouthWest = CheckSouthWest(x, y);

					if (nextSouthWest == true)
					{
						alreadyInRow++;
						if (alreadyInRow == PlaySettings.SlotsToWin)
						{
							return true; // Winner
						}
					}
				}
			}

			// West
			bool west = CheckWest(x, y);
			if (west == true)
			{
				int n = PlaySettings.SlotsToWin - 2;
				int alreadyInRow = 2;

				for (int i = 0; i < n; i++)
				{
					x--; // Move "left"

					bool nextWest = CheckWest(x, y);

					if (nextWest == true)
					{
						alreadyInRow++;
						if (alreadyInRow == PlaySettings.SlotsToWin)
						{
							return true; // Winner
						}
					}
				}
			}

			// North west
			bool northWest = CheckNorthWest(x, y);
			if (northWest == true)
			{
				int n = PlaySettings.SlotsToWin - 2;
				int alreadyInRow = 2;

				for (int i = 0; i < n; i++)
				{
					x--; // Move "left"
					y--; // Move "up"

					bool nextNorthWest = CheckNorthWest(x, y);

					if (nextNorthWest == true)
					{
						alreadyInRow++;
						if (alreadyInRow == PlaySettings.SlotsToWin)
						{
							return true; // Winner
						}
					}
				}
			}

			return false;
		}

		/*
		 * Checks if the player owns the tile north of the input tile.
		 * Param: x and y coordinates to start traversing from
		 * Returns: true if player owns the tile, false otherwise
		 */
		private bool CheckNorth(int x, int y)
		{
			// Since we are gonna check north, move y "up"
			y--;

			// Check for array out of bounds to avoid crash
			if (IsOutOfBounds(x, y) == true)
			{
				return false;
			}

			// Continue if the piece we wanna check isn't empty
			// This if statement avoids crash
			if (_pieces[x, y] != null)
			{
				GamePiece pieceToCheck = _pieces[x, y]; // Get the piece
				if (pieceToCheck.Owner.Equals(CurrentPlayer)) // Check if piece owner is the same as current player
				{
					// If so, we found two in a row
					return true;
				}
			}

			return false;
		}

		private bool CheckNorthEast(int x, int y)
		{
			y--; // Up
			x++; // Right

			if (IsOutOfBounds(x, y) == true)
			{
				return false;
			}

			if (_pieces[x, y] != null)
			{
				GamePiece pieceToCheck = _pieces[x, y];
				if (pieceToCheck.Owner.Equals(CurrentPlayer))
				{
					return true;
				}
			}

			return false;
		}


		private bool CheckEast(int x, int y)
		{
			x++; // Right

			if (IsOutOfBounds(x, y) == true)
			{
				return false;
			}

			if (_pieces[x, y] != null)
			{
				GamePiece pieceToCheck = _pieces[x, y];
				if (pieceToCheck.Owner.Equals(CurrentPlayer))
				{
					return true;
				}
			}

			return false;
		}

		private bool CheckSouthEast(int x, int y)
		{
			y++; // Down
			x++; // Right

			if (IsOutOfBounds(x, y) == true)
			{
				return false;
			}

			if (_pieces[x, y] != null)
			{
				GamePiece pieceToCheck = _pieces[x, y];
				if (pieceToCheck.Owner.Equals(CurrentPlayer))
				{
					return true;
				}
			}

			return false;
		}

		private bool CheckSouth(int x, int y)
		{
			y++; // Down

			if (IsOutOfBounds(x, y) == true)
			{
				return false;
			}

			if (_pieces[x, y] != null)
			{
				GamePiece pieceToCheck = _pieces[x, y];
				if (pieceToCheck.Owner.Equals(CurrentPlayer))
				{
					return true;
				}
			}

			return false;
		}

		private bool CheckSouthWest(int x, int y)
		{
			x--; // Left
			y++; // Down

			if (IsOutOfBounds(x, y) == true)
			{
				return false;
			}

			if (_pieces[x, y] != null)
			{
				GamePiece pieceToCheck = _pieces[x, y];
				if (pieceToCheck.Owner.Equals(CurrentPlayer))
				{
					return true;
				}
			}

			return false;
		}

		private bool CheckWest(int x, int y)
		{
			x--; // Left

			if (IsOutOfBounds(x, y) == true)
			{
				return false;
			}

			if (_pieces[x, y] != null)
			{
				GamePiece pieceToCheck = _pieces[x, y];
				if (pieceToCheck.Owner.Equals(CurrentPlayer))
				{
					return true;
				}
			}

			return false;
		}

		private bool CheckNorthWest(int x, int y)
		{
			x--; // Left
			y--; // Up

			if (IsOutOfBounds(x, y) == true)
			{
				return false;
			}

			if (_pieces[x, y] != null)
			{
				GamePiece pieceToCheck = _pieces[x, y];
				if (pieceToCheck.Owner.Equals(CurrentPlayer))
				{
					return true;
				}
			}

			return false;
		}

		/*
		 * Checks whether x or y is out of the array bounds
		 * Param: x and y coordinates
		 * Return: true if out of bounds, false if not
		 */
		private bool IsOutOfBounds(int x, int y)
		{
			if (x < 0)
			{
				return true;
			}
			else if (y < 0)
			{
				return true;
			}
			else if (x >= PlaySettings.BoardSize)
			{
				return true;
			}
			else if (y >= PlaySettings.BoardSize)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}