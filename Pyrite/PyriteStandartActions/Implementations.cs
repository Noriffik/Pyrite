﻿using PyriteStandartActions.CoreActionsUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PyriteStandartActions
{
    public static class Implementations
    {
        public static bool BeginUserSettings_RunExistingScenario(Dictionary<Guid, string> allScenarios, ref Guid currentScenGuid)
        {
            var form = new RunExistingScenarioActionView(allScenarios, currentScenGuid);
            if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                currentScenGuid = form.SelectedScenario;
                return true;
            }
            return false;
        }
    }
}
