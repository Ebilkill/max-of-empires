﻿using System;
using MaxOfEmpires.GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MaxOfEmpires.Units
{
    class TargetPositionOverlay : GameObjectList
    {
        private const double DRAW_INTERVAL_TIME = 0.05D;
        private const float MOVEMENT_SPEED = 16.0F;

        private Vector2 drawPos;
        private bool doneParticles;
        private int indexInPath;
        private Vector2 nextEnd;
        private Pathfinding.PathToTile path;
        private double timeUntilNextDraw;

        public TargetPositionOverlay(Unit u)
        {
            // Get the path along which to annotate the movement.
            path = Pathfinding.ShortestPath(u, u.TargetPosition);
            indexInPath = 0;

            // Set the initial position for drawing.
            drawPos = ToDrawPos(u.PositionInGrid);
            nextEnd = ToDrawPos(path.path[indexInPath]);

            // Set the timer correctly
            timeUntilNextDraw = DRAW_INTERVAL_TIME;

            // We're not done with drawing all particles yet...
            doneParticles = false;
        }

        /// <summary>
        /// Draws a particle at the current position. Also updates the center of the next square to move to, or stops when done.
        /// </summary>
        private void DrawNextParticle()
        {
            // If we're done with all particles, this should not be called.
            if (doneParticles)
                throw new Exception("Tried to draw more particles when all particles were drawn...");

            // If the current drawing is at the end of the path...
            if (drawPos == nextEnd)
            {
                // GO TOWARDS THE NEXT SQUARE
                ++indexInPath; // alright you don't have to shout at me

                // If this is the last square...
                if (indexInPath == path.path.Length)
                {
                    // Make sure we don't draw any more particles showing the path and just stop caring about everything.
                    doneParticles = true;
                    return;
                }

                // The next end is at the next point in the path
                nextEnd = ToDrawPos(path.path[indexInPath]);
            }

            // Move towards the next square and add a particle.
            MoveTowardsNextEnd();
            Add(new TargetPositionParticle(drawPos));
        }

        /// <summary>
        /// Moves the internal position closer towards the center of the next square along the path.
        /// </summary>
        private void MoveTowardsNextEnd()
        {
            // Get the direction from where we are to the next end.
            Vector2 direction = (nextEnd - drawPos);
            direction.Normalize();

            // Move the drawing position along the direction until we hit the target
            Vector2 newDrawPos = drawPos + direction * MOVEMENT_SPEED;

            // If we pass the nextEnd in either Y or X coords...
            if (Math.Sign(nextEnd.Y - drawPos.Y) != Math.Sign(nextEnd.Y - newDrawPos.Y) || Math.Sign(nextEnd.X - drawPos.X) != Math.Sign(nextEnd.X - newDrawPos.X))
            {
                // ... set the newDrawPos equal to the nextEnd
                newDrawPos = nextEnd;
            }

            // The draw position will be the new draw position
            drawPos = newDrawPos;
        }

        /// <summary>
        /// Converts a Point representing a Grid square to a point to draw a UnitTargetParticle.
        /// </summary>
        /// <param name="gridPos">The Grid square to convert.</param>
        /// <returns>The drawing position for a particle.</returns>
        private Vector2 ToDrawPos(Point gridPos)
        {
            Vector2 retVal = gridPos.ToVector2();

            retVal *= 32; // Size of a Grid square
            retVal += new Vector2(15); // In the middle of the square

            return retVal;
        }

        public override void Update(GameTime time)
        {
            base.Update(time);

            // Drawn all particles? Don't do anything else. 
            if (doneParticles)
                return;

            // Draw the next particle after an interval.
            timeUntilNextDraw -= time.ElapsedGameTime.TotalSeconds;
            while (timeUntilNextDraw < 0.0D)
            {
                timeUntilNextDraw += DRAW_INTERVAL_TIME;
                DrawNextParticle();
            }
        }

        public bool Finished => Count == 0 && doneParticles;
    }
}