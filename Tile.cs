﻿using MaxOfEmpires.Buildings;
using MaxOfEmpires.GameObjects;
using MaxOfEmpires.Units;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ebilkill.Gui;
using MaxOfEmpires.Files;
using System.IO;

namespace MaxOfEmpires
{
    class Tile : GameObjectDrawable
    {
        /// <summary>
        /// The current Building on this Tile. Can be null.
        /// </summary>
        private Building building;

        /// <summary>
        /// The Terrain of this Tile.
        /// </summary>
        private Terrain terrain;

        /// <summary>
        /// The current Unit on this Tile. Can be null.
        /// </summary>
        private Unit unit;

        /// <summary>
        /// Whether certain overlays should be drawn.
        /// </summary>
        private bool overlayAttack, overlayWalk;

        /// <summary>
        /// The x and y positions of this Tile in the containing Grid.
        /// </summary>
        private int x, y;

        public bool hills;

        public bool drawArrowNextTime;

        private Texture2D terrainTexture;

        private Texture2D arrowTexture;

        private Rectangle terrainSource; //TODO vervang door stuff

        private Rectangle arrowSource;

        /// <summary>
        /// Creates a new Tile at a specified position with a specified Terrain.
        /// </summary>
        /// <param name="terrain">The Terrain that this Tile should have.</param>
        /// <param name="x">The x-coord of the position that this Tile should be at.</param>
        /// <param name="y">The y-coord of the position that this Tile should be at.</param>
        public Tile(Terrain terrain, int x, int y)
        {
            this.terrain = terrain;
            this.x = x;
            this.y = y;
            position = new Vector2(x * 32, y * 32);
            overlayAttack = overlayWalk = false;
            terrainTexture = AssetManager.Instance.getAsset<Texture2D>("FE-Sprites/Terrain@5x4");
            arrowTexture = AssetManager.Instance.getAsset<Texture2D>("FE-Sprites/Arrow@7x2");
        }

        private void ArrowDraw(SpriteBatch s)
        {
            s.Draw(arrowTexture, DrawPosition, arrowSource, Color.White);
        }

        /// <summary>
        /// The movement cost for a specified Unit to move to this Tile.
        /// </summary>
        /// <param name="unit">The Unit for which you want to know the movement cost.</param>
        /// <returns>The movement cost on this Tile for the specified Unit.</returns>
        public int Cost(Unit unit)
        {
            if (!Passable(unit))
            {
                return int.MaxValue;
            }
            if (unit.id == "unit.builder")
            {
                return 1;
            }
            int terrainCost = terrain.Cost;
            if (!hills)
                return terrainCost;
            return 1+terrainCost;
        }

        public override void Draw(GameTime time, SpriteBatch s)
        {
            DrawBackground(time, s);
            DrawForeground(time, s);
        }

        public void DrawBackground(GameTime time, SpriteBatch s)
        {
            //terrain.Draw(DrawPosition.ToPoint(), s);
            TerrainSpriteSelect();
            TerrainDraw(s);
            building?.Draw(time, s);

            // Draw a walking overlay if it should be drawn
            if (drawArrowNextTime)
            {
                ArrowDraw(s);
                drawArrowNextTime = false;
            }

            if (overlayWalk)
            {
                DrawingHelper.Instance.DrawRectangle(s, Bounds, new Color(0x00, 0x00, 0xFF, 0x88));
            }

            // Draw an attacking overlay if it should be drawn
            if (overlayAttack)
            {
                DrawingHelper.Instance.DrawRectangle(s, Bounds, new Color(0xFF, 0x00, 0x00, 0x88));
            }
        }

        public void DrawForeground(GameTime time, SpriteBatch s)
        {
            unit?.Draw(time, s);
        }
        
        public void DrawArrow(Point relativeNext, Point relativePrevious)
        {
            drawArrowNextTime = true;
            if(relativePrevious == Point.Zero)
            {
                if (relativeNext.X == 1)
                {
                    SelectArrowSprite(1, 1);
                }
                else if (relativeNext.X == -1)
                {
                    SelectArrowSprite(2, 2);
                }
                else if (relativeNext.Y == 1)
                {
                    SelectArrowSprite(2, 1);
                }
                else
                {
                    SelectArrowSprite(1, 2);
                }
            }
            else if (relativeNext == Point.Zero)
            {
                if (relativePrevious.X == 1)
                {
                    SelectArrowSprite(7, 2);
                }
                else if (relativePrevious.X == -1)
                {
                    SelectArrowSprite(6, 1);
                }
                else if (relativePrevious.Y == 1)
                {
                    SelectArrowSprite(6, 2);
                }
                else
                {
                    SelectArrowSprite(7, 1);
                }
            }
            else if (relativeNext.X + relativePrevious.X == 0 && relativeNext.Y + relativePrevious.Y == 0)
            {
                if(relativeNext.X == 0)
                {
                    SelectArrowSprite(3, 1);
                }
                else
                {
                    SelectArrowSprite(3, 2);
                }
            }
            else
            {
                Point totalPoint = relativeNext + relativePrevious;
                if (totalPoint.X == totalPoint.Y)
                {
                    if(totalPoint.X == 1)
                    {
                        SelectArrowSprite(4, 1);
                    }
                    else
                    {
                        SelectArrowSprite(5, 2);
                    }
                }
                else{
                    if(totalPoint.X == 1)
                    {
                        SelectArrowSprite(4, 2);
                    }
                    else
                    {
                        SelectArrowSprite(5, 1);
                    }
                }
            }
        }

        public static Tile LoadFromFile(BinaryReader reader, int xPos, int yPos)
        {
            byte data = reader.ReadByte();
            bool hills = (data & 0x80) == 0x80;
            Terrain.TerrainType type = (Terrain.TerrainType)(data & (0x80 ^ 0xFF));

            Tile retVal = new Tile(Terrain.FromTerrainID((byte)type), xPos, yPos);
            retVal.hills = hills;
            return retVal;
        }

        private void SelectArrowSprite(int x, int y)
        {
            arrowSource = new Rectangle((x - 1) * 32, (y - 1) * 32, 32, 32);
        }

        private void SelectSprite(int x, int y)
        {
            terrainSource = new Rectangle((x - 1) * 32, (y - 1) * 32, 32, 32);
        }

        private void TerrainDraw(SpriteBatch s)
        {
            s.Draw(terrainTexture, DrawPosition, terrainSource, Color.White);
        }

        private void TerrainSpriteSelect()
        {
            if (!hills)
                SelectSprite(terrain.placeInSprite.X, terrain.placeInSprite.Y);
            else
                SelectSprite(terrain.placeInSprite.X-1, terrain.placeInSprite.Y);
        }

        /// <summary>
        /// Whether or not this Tile is passable for a certain unit. 
        /// </summary>
        /// <param name="unit">The Unit for which you want to know if this Tile is passable.</param>
        /// <returns>True if the Unit can pass through this Tile, false otherwise.</returns>
        public bool Passable(Unit unit)
        {
            // Enemy unit here
            if (Occupied && this.unit.Owner != unit.Owner)
            {
                if (this.unit is Soldier)
                {
                    // Enemy soldier here
                    if (!(this.unit as Soldier).IsDead)
                    {
                        // Alive? Can't pass
                        return false;
                    }
                    // Dead? Depends on terrain
                }
                else
                {
                    // Enemy unit that is not soldier, definitely alive
                    return false;
                }
            }
            return unit.Passable(terrain);
        }

        /// <summary>
        /// Sets a Unit on this tile. Also updates that Unit's GridPos to this Tile's position.
        /// </summary>
        /// <param name="u">The Unit to set on this Tile.</param>
        public void SetUnit(Unit u)
        {
            // Check to make sure the unit is not overriding another unit.
            if (Occupied && u != null)
                return;

            // Set the unit 
            unit = u;

            // Set the unit's position and parent, if it is not null
            if(unit != null)
            {
                unit.PositionInGrid = new Point(x, y);
                unit.Parent = this;
            }
        }

        public override void TurnUpdate(uint turn, Player player, GameTime t)
        {
            // Update the Unit at this position if it exists.
            if (Occupied)
                Unit.TurnUpdate(turn, player, t);

            // Update the Building at this position if it exists.
            if (BuiltOn)
                Building.TurnUpdate(turn, player, t);
        }

        public override void Update(GameTime time)
        {
            building?.Update(time);
            unit?.Update(time);
        }

        public void WriteToFile(BinaryWriter writer)
        {
            byte data = (byte)terrain.terrainType;
            data |= (byte)(hills ? 0x80 : 0);
            writer.Write(data);
        }

        /// <summary>
        /// The Building built on this Tile.
        /// </summary>
        public Building Building
        {
            get
            {
                return building;
            }
            set
            {
                building = value;
                if (value != null)
                    value.Parent = this;
            }
        }

        int CalculateDodgeBonus(Terrain.TerrainType type, bool containsHills)
        {
            Configuration file = FileManager.LoadConfig("configs/terrain/terrainBonusses");
            int bonus = file.GetProperty<int>(type.ToString().ToLower()+".dodge");
            if(hills)
                bonus+= file.GetProperty<int>("hills.dodge");
            return bonus;
        }

        int CalculateDefenseBonus(Terrain.TerrainType type, bool containsHIlls)
        {
            Configuration file = FileManager.LoadConfig("configs/terrain/terrainBonusses");
            int bonus = file.GetProperty<int>(type.ToString().ToLower() + ".defense");
            if (hills)
                bonus += file.GetProperty<int>("hills.defense");
            return bonus;
        }

        /// <summary>
        /// Whether there is a Building on this Tile.
        /// </summary>
        public bool BuiltOn => building != null;

        /// <summary>
        /// The position in the Grid of this Tile.
        /// </summary>
        public Point PositionInGrid => new Point(x, y);

        /// <summary>
        /// Whether there is a Unit on this Tile.
        /// </summary>
        public bool Occupied => Unit != null;

        public int DefenseBonus => CalculateDefenseBonus(terrain.terrainType, hills);

        public int DodgeBonus => CalculateDodgeBonus(terrain.terrainType, hills);

        /// <summary>
        /// True when the attacking overlay on this tile should be shown, false otherwise.
        /// </summary>
        public bool OverlayAttack
        {
            get
            {
                return overlayAttack;
            }
            set
            {
                overlayAttack = value;
            }
        }

        /// <summary>
        /// True when the walking overlay on this tile should be shown, false otherwise.
        /// </summary>
        public bool OverlayWalk
        {
            get
            {
                return overlayWalk;
            }
            set
            {
                overlayWalk = value;
            }
        }

        public override Vector2 Size
        {
            get
            {
                return new Vector2(32, 32);
            }
        }

        /// <summary>
        /// The Terrain of this Tile.
        /// </summary>
        public Terrain Terrain {
            get
            {
                return terrain;
            }
            set
            {
                terrain = value;
            }
        }

        /// <summary>
        /// The Unit occupying this Tile.
        /// </summary>
        public Unit Unit
        {
            get
            {
                return unit;
            }
        }
    }
}
