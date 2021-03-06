﻿using MaxOfEmpires.GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace MaxOfEmpires.Units
{
    abstract class Unit : GameObjectAnimated
    {
        /// <summary>
        /// Whether this unit has attacked. Units can only attack once.
        /// </summary>
        protected bool hasAttacked;

        /// <summary>
        /// The amount of squares this unit can still move.
        /// </summary>
        protected int movesLeft;

        /// <summary>
        /// The total amount of squares a unit can move each turn.
        /// </summary>
        protected int moveSpeed;

        public string id;

        protected Player owner; // 2 is false, 1 is true

        /// <summary>
        /// The target position for this Unit.
        /// </summary>
        private Point target;

        /// <summary>
        /// The x and y coords of this Unit. Used for drawing and moving.
        /// </summary>
        protected int x, y;

		
        /// <summary>
        /// Whenever the unit needs to move, he will pass these vectors one after another, setting out a path to follow.
        /// Needs to be empty (constructor) when the unit is created, as there is no initial path to follow.
        /// </summary>
        private List<Vector2> vectors;

        public Unit(int x, int y, Player owner, string id = "") : base(false, true, 0.3D)
        {
            this.id = id;
            this.x = x;
            this.y = y;
            this.owner = owner;
            target = new Point(x, y);

            vectors = new List<Vector2>();
        }

        /// <summary>
        /// Calculates the distance from this Unit's position to the specified position.
        /// </summary>
        /// <param name="x">The x-coord of the new position.</param>
        /// <param name="y">The y-coord of the new position.</param>
        /// <returns>The total distance between the specified position and this Unit's position.</returns>
        protected int DistanceTo(int x, int y)
        {
            int xDist = Math.Abs(x - this.x);
            int yDist = Math.Abs(y - this.y);
            return xDist + yDist;
        }

        public override void Draw(GameTime time, SpriteBatch s)
        {
            // Draw the Unit based on whether it still has moves left; gray if there are no moves left.
            DrawColor = HasMoved ? new Color(Color.Gray, DrawColor.A / 255F) : new Color(Color.White, DrawColor.A / 255F);
            base.Draw(time, s);
        }

        /// <summary>
        /// Checks if this Unit can move to the specified position. Returns false if this Unit can't reach the specified position.
        /// Assumes the specified position is not occupied.
        /// </summary>
        /// <param name="x">The x-coord of the position to move to.</param>
        /// <param name="y">The y-coord of the position to move to.</param>
        /// <returns>True if the Unit moved to the position, false otherwise.</returns>
        public virtual bool Move(int x, int y)
        {
            // Can't move to our own spot, right?
            if (new Point(x, y).Equals(PositionInGrid))
                return false;

            // If we already moved, we can't move anymore. Something like that
            if (HasMoved)
            {
                return false;
            }

            // Get the distance to the specified position.
            int distance = Pathfinding.ShortestPath(this, new Point(x, y)).cost;

            // Check if we can move to this position. Decrements moves left as well if we can.
            if (distance <= movesLeft)
            {
                movesLeft -= distance;
                return true;
            }
            return false;
        }

        public virtual bool Passable(Terrain terrain)
        {
            return !(terrain == Terrain.Mountain || terrain == Terrain.Lake || terrain == Terrain.DesertMountain || terrain == Terrain.TundraMountain);
        }

        public override void TurnUpdate(uint turn, Player player, GameTime t)
        {
            if (owner == player)
            {
                movesLeft = moveSpeed;
            }
            ShouldAnimate = player == owner;
        }

        public virtual void WriteToFile(BinaryWriter writer)
        {
            // Write the x, y, and owner
            writer.Write((short)x);
            writer.Write((short)y);
            writer.Write(owner.Name);

            // Write moves left, id, and target
            writer.Write((byte)movesLeft);
            writer.Write(id);
            writer.Write((short)target.X);
            writer.Write((short)target.Y);
        }

        /// <summary>
        /// Whether this unit can still perform an action this turn. 
        /// </summary>
        public virtual bool HasAction => !HasMoved;

        /// <summary>
        /// Whether this Unit can still move this turn.
        /// </summary>
        public bool HasMoved
        {
            get
            {
                return MovesLeft <= 0;
            }
            set
            {
                if (value)
                    movesLeft = 0;
            }
        }

        /// <summary>
        /// The amount of tiles this Unit can still run this turn.
        /// </summary>
        public int MovesLeft
        {
            get
            {
                return movesLeft;
            }
            set
            {
                movesLeft = value;
            }
        }

        /// <summary>
        /// The amount of tiles this Unit can run each turn.
        /// </summary>
        public int MoveSpeed
        {
            get
            {
                return moveSpeed;
            }
            set
            {
                moveSpeed = value;
            }
        }

        /// <summary>
        /// The owner of this Unit. True => player 1, false => player 2.
        /// </summary>
        public Player Owner
        {
            get
            {
                return owner;
            }
            set
            {
                owner = value;
            }
        }

        /// <summary>
        /// The position in the Grid this Unit occupies.
        /// </summary>
        public Point PositionInGrid
        {
            get
            {
                return new Point(x, y);
            }
            set
            {
                this.x = value.X;
                this.y = value.Y;
            }
        }

        /// <summary>
        /// Target location.
        /// </summary>
        public Point TargetPosition
        {
            get
            {
                return target;
            }
            set
            {
                if ((GameWorld == null || GameWorld is Unit) || (GameWorld as Grid).IsInGrid(value))
                {
                    target = value;
                }
            }
        }

        public List<Vector2> Vectors
        {
            get
            {
                return vectors;
            }
            set
            {
                vectors = value;
            }
        }
    }
}
