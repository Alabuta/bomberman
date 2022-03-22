using System;
using Game;
using Math.FixedPointMath;

namespace Level
{
    public partial class World
    {
        private void OnPlayerBombPlant(IPlayer player, fix2 worldPosition)
        {
            var bombConfig = player.Hero.BombConfig;
            var bombCoordinate = LevelGridModel.ToTileCoordinate(worldPosition, true);
            var position = LevelGridModel.ToWorldPosition(bombCoordinate);

            _gameFactory.InstantiatePrefab(bombConfig.Prefab, fix2.ToXY(position));
        }

        private void OnPlayerBombBlast(IPlayer player)
        {
            throw new NotImplementedException();
        }

        // :TODO: add an item pick up event handler
        // :TODO: add an entity death event handler
    }
}
