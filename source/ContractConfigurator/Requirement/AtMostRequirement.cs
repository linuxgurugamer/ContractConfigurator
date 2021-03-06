﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator.ExpressionParser;
using ContractConfigurator.Util;
using KSP.Localization;

namespace ContractConfigurator
{
    /// <summary>
    /// Set requirement to check if at most a given number of child requirements are met.
    /// </summary>
    public class AtMostRequirement : ContractRequirement
    {
        protected int count;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "count", x => count = x, this, x => Validation.GE<int>(x, 0));

            // Not invertable
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "invertRequirement", x => invertRequirement = x, this, false, x => Validation.EQ(x, false));

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            int metCount = 0;
            foreach (ContractRequirement requirement in childNodes)
            {
                if (requirement.enabled)
                {
                    if (requirement.CheckRequirement(contract))
                    {
                        metCount++;
                    }

                }
            }

            return metCount <= count;
        }

        public override void OnLoad(ConfigNode configNode)
        {
            count = ConfigNodeUtil.ParseValue<int>(configNode, "count");
        }

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("count", count);
        }

        protected override string RequirementText()
        {
            return Localizer.Format("#cc.req.AtMost", MissionControlUI.RequirementHighlightColor, count);
        }
    }
}
