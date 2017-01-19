﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ebilkill.Gui.Elements;
using Microsoft.Xna.Framework;

namespace MaxOfEmpires.Buildings
{
    class Academy : Building
    {
        public Academy(Point positionInGrid, Player owner) : base(positionInGrid, owner)
        {
        }

        public override void PopulateBuildingActions(GuiList buildingActions)
        {
            // Get this building's trainees
            IList<string> trainees = BuildingRegistry.GetTrainees("building.academy");

            // Add a button for every trainee
            foreach (string trainee in trainees)
            {
                AddRecruitingButton(buildingActions, trainee);
            }

            // Add the basic building actions
            base.PopulateBuildingActions(buildingActions);
        }
    }
}