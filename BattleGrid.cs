﻿using MaxOfEmpires.GameStates;
using MaxOfEmpires.Units;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace MaxOfEmpires
{
    class BattleGrid : Grid
    {
        private Player attackingPlayer;
        private Player defendingPlayer;

        public BattleGrid(int width, int height, List<Player> players, string id = "") : base(width, height, players, id) // TODO: make this load from procedural generation.
        {
        }

        /// <summary>
        /// Checks whether a Unit can attack a Unit at the specified tile, and attacks it if it's possible.
        /// </summary>
        /// <param name="newPos">The position for the Unit to attack.</param>
        /// <param name="unit">The Unit which attacks.</param>
        /// <returns>True if the Unit attacked, false otherwise.</returns>
        public bool CheckAttackSoldier(Point tileToAttack, Soldier attackingUnit)
        {
            // Cannot attack more than once a turn. 
            if (attackingUnit.HasAttacked)
                return false;

            Tile toAttack = this[tileToAttack] as Tile;

            // Make sure the attack square is occupied by an enemy unit
            if (!toAttack.Occupied || toAttack.Unit.Owner == attackingUnit.Owner)
            {
                return false; // nothing to attack
            }

            // Make sure the attack square is in range of the attacking unit
            if (!attackingUnit.IsInRange(tileToAttack))
            {
                return false; // Enemy not in range
            }

            // We can actually attack this? Nice :D
            attackingUnit.Attack(toAttack);

            // After a battle, check if there are dead Units, and remove these if they are dead
            Soldier defender = toAttack.Unit as Soldier;
            if (defender.IsDead)
            {
                OnKillSoldier(defender);
            }
            if (attackingUnit.IsDead)
            {
                OnKillSoldier(attackingUnit);
            }

            return true;
        }

        public override void Draw(GameTime time, SpriteBatch s)
        {
            base.Draw(time, s);
        }

        /// <summary>
        /// Initializes the field.
        /// </summary>
        public override void InitField()
        {
            // Initialize the terrain
            for (int x = 0; x < Width; ++x)
            {
                for (int y = 0; y < Height; ++y)
                {
                    this[x, y] = new Tile(Terrain.Plains, x, y);
                }
            }
        }

        /// <summary>
        /// Called when a Soldier dies in battle.
        /// </summary>
        /// <param name="deadSoldier">The Soldier that died.</param>
        private void OnKillSoldier(Soldier deadSoldier)
        {
            // Remove this Soldier as it is dead
            (this[deadSoldier.PositionInGrid] as Tile).SetUnit(null);

            // If there are no Soldiers left on this side, the enemy won D:
            bool foundAlly = false;

            // Check all tiles
            ForEach((obj, x, y) => {
                Tile t = obj as Tile;

                // If there is an ally, update this condition
                if (t.Occupied && t.Unit.Owner == deadSoldier.Owner)
                {
                    foundAlly = true;
                }
            });

            // If we found no ally, the enemy won.
            if (!foundAlly)
            {
                OnPlayerWinBattle(deadSoldier.Owner == attackingPlayer ? defendingPlayer : attackingPlayer);
            }
        }

        /// <summary>
        /// Executed when the player left-clicks on the grid.
        /// </summary>
        /// <param name="helper">The InputHelper used for mouse input.</param>
        public override void OnLeftClick(InputHelper helper)
        {
            // Get the current Tile under the mouse
            Tile clickedTile = GetTileUnderMouse(helper, true);

            // Do nothing if there is no clicked tile.
            if (clickedTile == null)
                return;

            // If the player had a tile selected and it contains a Unit...
            if (SelectedTile != null && SelectedTile.Occupied)
            {
                // ... move the Unit there, if the square is not occupied and the unit is capable, then unselect the tile.
                SelectedTile.Unit.TargetPosition = clickedTile.GridPos;
                Point movePos = Pathfinding.MoveTowardsTarget(SelectedTile.Unit);

                if (CheckMoveUnit(movePos, SelectedTile.Unit) || CheckAttackSoldier(clickedTile.GridPos, (Soldier)SelectedTile.Unit))
                {
                    SelectTile(InvalidTile);
                    return;
                }
            }

            // Check if the player clicked a tile with a Unit on it, and select it if it's there. 
            else if (clickedTile.Occupied && clickedTile.Unit.Owner == currentPlayer && clickedTile.Unit.HasAction)
            {
                // If the Unit can walk, show where it is allowed to walk. 
                if (!clickedTile.Unit.HasMoved)
                {
                    Point[] walkablePositions = Pathfinding.ReachableTiles(clickedTile.Unit);
                    SetUnitWalkingOverlay(walkablePositions);
                }

                // This unit can be selected. Show the player it is selected too
                SelectTile(clickedTile.GridPos);

                // Add an overlay for enemy units that can be attacked
                if (!(clickedTile.Unit as Soldier).HasAttacked)
                {
                    SetUnitAttackingOverlay((Soldier)clickedTile.Unit);
                }
            }
        }

        /// <summary>
        /// Called when a player wins a battle (because all enemies died).
        /// </summary>
        /// <param name="winningPlayer">The player that won the battle.</param>
        private void OnPlayerWinBattle(Player winningPlayer)
        {
            // Unset attacker and defender
            attackingPlayer = null;
            defendingPlayer = null;

            // Find what's left of our army
            Army remainingArmy = new Army(0, 0, winningPlayer);
            ForEach((obj, x, y) => {
                Tile t = obj as Tile;

                // See if there's a Soldier belonging to the winning player here
                if (t.Occupied && t.Unit.Owner == winningPlayer)
                {
                    remainingArmy.AddSoldier(t.Unit as Soldier);
                }
            });

            // Tell economy grid what's left and go back to economy grid
            GameStateManager.OnPlayerWinBattle(remainingArmy);
        }

        /// <summary>
        /// Populates the field with the armies of both attacker and defender.
        /// </summary>
        /// <param name="attacker">The attacking Army.</param>
        /// <param name="defender">The defending Army.</param>
        public void PopulateField(Army attacker, Army defender)
        {
            // Initialize which players are the attacker and the defender
            attackingPlayer = attacker.Owner;
            defendingPlayer = defender.Owner;

            // Initialize the attacker's field
            int currentX = 0;
            int currentY = 0;

            // Iterate over every type of Soldier in the attacking Army
            foreach (string s in attacker.UnitsAndCounts.Keys)
            {
                // Get the amount of this kind of Soldier
                int soldierCount = attacker.UnitsAndCounts[s];

                // Place them in a position based on how many soldiers we have passed so far.
                for (; soldierCount > 0; --soldierCount)
                {
                    (this[currentX, currentY] as Tile).SetUnit(SoldierRegistry.GetSoldier(s, attacker.Owner));
                    ++currentX;

                    // Make sure we don't create a line of soldiers longer than the field.
                    if (currentX >= Width)
                    {
                        currentX = 0;
                        ++currentY;
                    }
                }
            }

            // Do the same for the defending Army, except start bottom right and go left->up for each Soldier.
            currentX = Width - 1;
            currentY = Height - 1;

            foreach (string s in defender.UnitsAndCounts.Keys)
            {
                int soldierCount = defender.UnitsAndCounts[s];

                for (; soldierCount > 0; --soldierCount)
                {
                    (this[currentX, currentY] as Tile).SetUnit(SoldierRegistry.GetSoldier(s, defender.Owner));
                    --currentX;

                    if (currentX < 0)
                    {
                        currentX = Width - 1;
                        --currentY;
                    }
                }
            }

            // And clear all target positions after we populated the field.
            ClearAllTargetPositions();
            currentPlayer = attacker.Owner;
        }
    }
}
