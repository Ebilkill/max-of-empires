﻿using MaxOfEmpires.GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ebilkill.Gui.Elements;
using MaxOfEmpires.GameStates.Overlays;
using MaxOfEmpires.Units;
using System.Text;
using MaxOfEmpires.Files;
using System.IO;
using System;
using System.Collections.Generic;

namespace MaxOfEmpires.Buildings
{
    class Building : GameObjectDrawable
    {
        private Player owner;
        private Point positionInGrid;
        public readonly string buildingName;
        private int turnsSeized;
        protected int turnsBeforeRazeOnSeize;

        public Building(Point positionInGrid, Player owner, string buildingName)
        {
            this.positionInGrid = positionInGrid;
            this.owner = owner;
            this.buildingName = buildingName;
            turnsSeized = 0;
            turnsBeforeRazeOnSeize = BuildingRegistry.GetRazeTime(buildingName);
            LoadTexture();
        }

        protected void AddRecruitingButton(GuiList buildingActions, string unitToRecruit)
        {
            StringBuilder labelNextToButtonText = new StringBuilder();
            labelNextToButtonText.Append(Translations.GetTranslation(unitToRecruit)).Append(" ("); // Soldier (
            labelNextToButtonText.Append(SoldierRegistry.GetSoldierCost(unitToRecruit)).Append("G): "); // Soldier ('cost'G)
            buildingActions.addElement(ElementBuildButton.CreateBuildButton(buildingActions.Bounds.Location, labelNextToButtonText.ToString(), () => TrySpawnUnit(unitToRecruit),"Recruit"));
        }

        protected void AddUpgradeButton(GuiList buildingActions, string unitToUpgrade, int tier, Player player)
        {
            StringBuilder buttonText = new StringBuilder();
            buttonText.Append("Upgrade to tier ").Append(tier+1).Append(": (");
            buttonText.Append(SoldierRegistry.GetUpgradeCost(unitToUpgrade, player.soldierTiers[unitToUpgrade])).Append("G): "); // Soldier ('cost'G)
            buildingActions.addElement(ElementBuildButton.CreateBuildButton(buildingActions.Bounds.Location, buttonText.ToString(), () => { TryUpgradeUnit(unitToUpgrade); },"Upgrade"));
        }

        public override void Draw(GameTime time, SpriteBatch s)
        {
            base.Draw(time, s);
        }

        public static void LoadFromConfig(Configuration buildingConfiguration)
        {
            Capital.LoadFromConfig(buildingConfiguration);
            Town.LoadFromConfig(buildingConfiguration);
            Mine.LoadFromConfig(buildingConfiguration);
        }

        public static Building LoadFromFile(BinaryReader reader, List<Player> players)
        {
            // Read the position of the Building
            short x = reader.ReadInt16();
            short y = reader.ReadInt16();

            // Read who the owner is
            string owner = reader.ReadString();

            // Read which building it is
            string buildingID = reader.ReadString();

            Building building = (Building)Activator.CreateInstance(BuildingRegistry.buildingTypeById[buildingID], new object[] { new Point(x, y), players.Find(p => p.Name.Equals(owner)) });
            
            // Everything in constructor has been loaded, so just the time we've been seized left
            building.turnsSeized = reader.ReadInt16();

            return building;
        }

        private void LoadTexture()
        {
            StringBuilder textureName = new StringBuilder();
            textureName.Append("FE-Sprites/Buildings/");
            textureName.Append(BuildingRegistry.GetTextureName(buildingName));
            textureName.Append("@1x2");

            DrawingTexture = AssetManager.Instance.getAsset<Spritesheet>(textureName.ToString());
            DrawingTexture.SelectedSprite = new Point(0, owner.ColorName.ToLower().Equals("blue") ? 0 : 1);
        }

        public virtual void PopulateBuildingActions(GuiList buildingActions)
        {
        }

        public virtual void RazeBuilding(Unit destroyer)
        {
            // Destroy the building
            Tile t = (GameWorld as Grid)[PositionInGrid] as Tile;
            t.Building = null;

            owner.AddBuildingLostToStats(buildingName);

            // Also re-update the population because that's what should be done after a town is destroyed
            Owner.CalculatePopulation();
            Owner.CalculateMoneyPerTurn();
        }

        protected void TrySpawnUnit(string soldierType)
        {
            // Check if the player can afford this soldier
            int cost = SoldierRegistry.GetSoldierCost(soldierType);
            if (!owner.CanAfford(cost) || owner.Population <= 0)
            {
                return;
            }

            // Set this soldier in the world if possible
            Tile currentTile = ((GameWorld as Grid)[positionInGrid] as Tile);
            if (!currentTile.Occupied)
            {
                // Nothing here, just place it in this square
                Army army = new Army(positionInGrid.X, positionInGrid.Y, owner);
                army.AddSoldier(SoldierRegistry.GetSoldier(soldierType, owner));
                currentTile.SetUnit(army);
            }
            else if (currentTile.Unit.Owner == owner && currentTile.Unit is Army)
            {
                // Our own army is here, just place it in there :)
                Army a = currentTile.Unit as Army;
                if (a.GetTotalUnitCount() >= 20)
                {
                    return;
                }
                a.AddSoldier(SoldierRegistry.GetSoldier(soldierType, owner));
            }
            else
            {
                // We can't place it, just stop this whole function
                return;
            }

            // Buy the soldier, as we placed it.
            owner.Buy(cost);
            owner.CalculatePopulation();
        }

        protected void TryUpgradeUnit(string soldierType)
        {
            int cost = SoldierRegistry.GetUpgradeCost(soldierType, owner.soldierTiers[soldierType]);
            if (!owner.CanAfford(cost))
            {
                return;
            }
            owner.Buy(cost);
            owner.soldierTiers[soldierType]+=1;
        }

        public override void TurnUpdate(uint turn, Player player, GameTime time)
        {
            // Check to see if an enemy ARMY is here
            Tile t = (GameWorld as Grid)[PositionInGrid] as Tile;
            if (!t.Occupied || t.Unit.Owner == owner || !(t.Unit is Army))
            {
                // We're no longer being seized, I suppose :D
                turnsSeized = 0;
                return;
            }

            // We need the end of the other player's turn to increment the counter
            if (player != Owner)
            {
                return;
            }

            // See if we're being destroyed by the enemy army :/
            if (turnsSeized >= turnsBeforeRazeOnSeize)
            {
                RazeBuilding(t.Unit);
                return;
            }

            // Omg no we're being seized D:
            ++turnsSeized;
        }

        public void WriteToFile(BinaryWriter stream)
        {
            // Write the position
            stream.Write((short)PositionInGrid.X);
            stream.Write((short)PositionInGrid.Y);

            // Write the owning player's name
            stream.Write(owner.Name);

            // Write which building this is
            stream.Write(buildingName);

            // Write how long it's been seized already
            stream.Write((short)turnsSeized);
        }

        public Player Owner => owner;
        public Point PositionInGrid => positionInGrid;
    }
}
