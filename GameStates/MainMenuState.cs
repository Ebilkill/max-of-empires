﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MaxOfEmpires.GameStates.Overlays;

namespace MaxOfEmpires.GameStates
{
    class MainMenuState : GameState
    {
        private OverlayMainMenu overlay;

        public MainMenuState()
        {
            overlay = new OverlayMainMenu();
        }

        public override void Draw(GameTime time, SpriteBatch s)
        {
            overlay.draw(s);
        }

        public override void Update(GameTime time)
        {
        }

        public override void HandleInput(InputHelper helper, KeyManager manager)
        {
            overlay.update(helper);
        }
    }
}