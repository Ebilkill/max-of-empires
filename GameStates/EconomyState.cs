﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaxOfEmpires.Units;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MaxOfEmpires.GameStates
{
    class EconomyState : GameState
    {
        public const int GRID_WIDTH = 36;
        public const int GRID_HEIGHT = 36;

        private List<Player> players;
        private int currentPlayer;
        private EconomyGrid ecoGrid;
        private Overlays.OverlayEconomyState overlay;
        private bool shouldTurnUpdate;
        private uint turnNum;

        public EconomyState(Player blue, Player red)
        {
            players = new List<Player>();
            players.Add(blue);
            players.Add(red);
            currentPlayer = 0;

            ResetOverlay();

            AssetManager.Instance.PlayMusic("Music/Village of Emerdale");
        }

        public override void Draw(GameTime time, SpriteBatch gameObjectS, SpriteBatch overlayS)
        {
            // Draw the economy grid and then the overlay, followed by possible fading (in/out)
            ecoGrid.Draw(time, gameObjectS);
            overlay.draw(overlayS);
            base.Draw(time, gameObjectS, overlayS);
        }

        public override void HandleInput(InputHelper helper, KeyManager manager)
        {
            // Mouse over overlay
            if (new Rectangle(MaxOfEmpires.overlayPos.ToPoint(), MaxOfEmpires.ScreenSize).Contains(helper.GetMousePosition(false)))
            {
                // Update the overlay
                overlay.update(helper);
            }

            // Mouse over grid (we hope >_>)
            else
            {
                // Handle input for the grid (things like moving and attacking units)
                ecoGrid.HandleInput(helper, manager);
            }

            // Get the Unit under the mouse...
            Unit unitUnderMouse = ecoGrid.GetTileUnderMouse(helper)?.Unit;

            // ... and the selected Unit...
            Unit selectedUnit = ecoGrid.SelectedTile?.Unit;

            // Show nothing by default
            PrintBuilderInfo(null);

            // ... and print their information if it's an Army...
            if (unitUnderMouse is Army)
            {
                PrintArmyInfo((Army)unitUnderMouse);
            }
            else if (selectedUnit is Army)
            {
                PrintArmyInfo((Army)selectedUnit);
            }
            else
            {
                PrintArmyInfo(null);
            }

            // ... or set the building information if it's a Builder... 
            if (selectedUnit is Builder && !ecoGrid.SelectedTile.BuiltOn)
            {
                PrintBuilderInfo((Builder)selectedUnit);
            }

            // ... show the information of the currently selected Building...
            PrintBuildingInfo(ecoGrid.SelectedTile?.Building);

            if (manager.KeyPressed("nextTurn", helper))
            {
                shouldTurnUpdate = true;
            }
        }

        private void InitOverlay()
        {
            overlay.EndTurnHandler = () => { shouldTurnUpdate = true; };
            overlay.InitBuildingList(ecoGrid);

            foreach (Player p in players)
            {
                p.OnUpdateMoney(UpdateMoneyDisplay);
                p.OnUpdatePopulation(UpdatePopulationDisplay);
                p.OnUpdateMoneyPerTurn(UpdateMoneyPerTurnDisplay);
            }
        }

        /// <summary>
        /// Called when a player wins a battle in the battle state.
        /// </summary>
        /// <param name="remainingArmy">The remaining army after the battle has ended.</param>
        public void OnPlayerWinBattle(Army remainingArmy)
        {
            ecoGrid.OnPlayerWinBattle(remainingArmy);
        }

        private void PrintArmyInfo(Army a)
        {
            overlay.PrintArmyInfo(a);
        }

        private void PrintBuilderInfo(Builder builder)
        {
            overlay.PrintBuilderInfo(builder);
        }

        private void PrintBuildingInfo(Buildings.Building building)
        {
            overlay.PrintBuildingInfo(building);
        }

        public override void Reset()
        {
            // Reset the players
            foreach (Player p in players)
                p.Reset();

            // Initialize the field
            EconomyGrid = new EconomyGrid(GRID_WIDTH, GRID_HEIGHT, players);
            ecoGrid.InitField();

            // Player 1 starts
            currentPlayer = 0;

            // Turn number starts at 1, so 0 and double TurnUpdate
            turnNum = 0;
            TurnUpdate(null);
            TurnUpdate(null);
        }

        private void SelectNextPlayer()
        {
            CurrentPlayer.EcoCameraPosition = MaxOfEmpires.camera.Position;
            CurrentPlayer.ZoomValue = MaxOfEmpires.camera.Zoom;
            ++currentPlayer;
            if (currentPlayer >= players.Count)
            {
                currentPlayer = 0;
                ++turnNum;
            }
            MaxOfEmpires.camera.Position = CurrentPlayer.EcoCameraPosition;
            MaxOfEmpires.camera.Zoom = CurrentPlayer.ZoomValue;
        }

        public override void ResetOverlay()
        {
            overlay = new Overlays.OverlayEconomyState();
            InitOverlay();
            UpdateGuiInformation();
        }

        /// <summary>
        /// Called when the turn is updated. Sets the current player to the other player and then calls Grid.TurnUpdate.
        /// </summary>
        public void TurnUpdate(GameTime t)
        {
            SelectNextPlayer();
            ecoGrid.TurnUpdate(turnNum, CurrentPlayer, t);
            UpdateGuiInformation();
            if (turnNum == 1 && CurrentPlayer == players[1])
            {
                MaxOfEmpires.camera.setDownRight();
            }
        }

        public override void Update(GameTime time)
        {
            base.Update(time);
            ecoGrid.Update(time);
            overlay.timeSinceSwitch = Math.Min(overlay.timeSinceSwitch + time.ElapsedGameTime.TotalSeconds * 5, 1.1D);
            if (shouldTurnUpdate && !ecoGrid.ContainsWalkingUnits)
            {
                shouldTurnUpdate = false;
                TurnUpdate(time);
            }
        }

        private void UpdateGuiInformation()
        {
            // Sidebar color
            overlay.oldColor = overlay.playerColor;
            overlay.playerColor = CurrentPlayer.Color;
            overlay.timeSinceSwitch = 0.0D;

            // Sidebar info
            overlay.LabelCurrentPlayer.setLabelText("Current player: " + CurrentPlayer.Name);
            UpdateMoneyDisplay(CurrentPlayer);
            UpdatePopulationDisplay(CurrentPlayer);
            UpdateMoneyPerTurnDisplay(CurrentPlayer);
        }

        private void UpdateMoneyDisplay(Player p)
        {
            overlay.LabelPlayerMoney.setLabelText("Money: " + p.Money + "G");
        }

        private void UpdateMoneyPerTurnDisplay(Player p)
        {
            overlay.LabelPlayerMoneyPerTurn.setLabelText("Money per turn: " + p.MoneyPerTurn + "G");
        }

        public void UpdatePopulationDisplay(Player p)
        {
            overlay.LabelPlayerPopulation.setLabelText("Free Population: " + p.Population);
        }

        public List<Player> Players
        {
            get
            {
                return players;
            }
            set
            {
                players = value;
                foreach (Player p in players)
                {
                    p.OnUpdateMoney(UpdateMoneyDisplay);
                    p.OnUpdatePopulation(UpdatePopulationDisplay);
                    p.OnUpdateMoneyPerTurn(UpdateMoneyPerTurnDisplay);
                    p.CalculateMoneyPerTurn();
                    p.CalculatePopulation();
                }
                EconomyGrid.CurrentPlayer = CurrentPlayer;
            }
        }

        private Player CurrentPlayer => players[currentPlayer];

        public EconomyGrid EconomyGrid
        {
            get
            {
                return ecoGrid;
            }
            set
            {
                ecoGrid = value;
                ResetOverlay();
            }
        }
    }
}
