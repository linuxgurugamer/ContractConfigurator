﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using KSP.Localization;
using RemoteTech;
using ContractConfigurator;

namespace ContractConfigurator.RemoteTech
{
    /// <summary>
    /// ContractRequirement to check whether a celestial body has coverage.
    /// </summary>
    public class CelestialBodyCoverageRequirement : ContractRequirement
    {
        protected double minCoverage;
        protected double maxCoverage;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            // Before loading, verify the RemoteTech version
            valid &= Util.Version.VerifyRemoteTechVersion();

            // Do not check on active contracts
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : false;

            // Not invertable
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "invertRequirement", x => invertRequirement = x, this, false, x => Validation.EQ(x, false));

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minCoverage", x => minCoverage = x, this, 0.0, x => Validation.BetweenInclusive(x, 0.0, 1.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxCoverage", x => maxCoverage = x, this, 1.0, x => Validation.BetweenInclusive(x, 0.0, 1.0));
            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("minCoverage", minCoverage);
            configNode.AddValue("maxCoverage", maxCoverage);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            minCoverage = ConfigNodeUtil.ParseValue<double>(configNode, "minCoverage", 0.0);
            maxCoverage = ConfigNodeUtil.ParseValue<double>(configNode, "maxCoverage", 1.0);
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            LoggingUtil.LogVerbose(this, "Checking requirement");

            // Perform another validation of the target body to catch late validation issues due to expressions
            if (!ValidateTargetBody())
            {
                return false;
            }

            if (RemoteTechProgressTracker.Instance == null)
            {
                // Assume no coverage
                return maxCoverage == 0.0;
            }

            double coverage = RemoteTechProgressTracker.GetCoverage(targetBody);
            return coverage >= minCoverage && coverage <= maxCoverage;
        }

        protected override string RequirementText()
        {
            string body = targetBody == null ? Localizer.GetStringByTag("#cc.req.ProgressCelestialBody.genericBody") : targetBody.displayName;
            if (minCoverage > 0 && maxCoverage < 1.0)
            {
                return Localizer.Format("#cc.scansat.req.SCANsatCoverage.between", (minCoverage * 100).ToString("N0"), (maxCoverage * 100).ToString("N0"), "RemoteTech", body);
            }
            else if (minCoverage > 0)
            {
                return Localizer.Format("#cc.scansat.req.SCANsatCoverage.atLeast", (minCoverage * 100).ToString("N0"), "RemoteTech", body);
            }
            else
            {
                return Localizer.Format("#cc.scansat.req.SCANsatCoverage.atMost", (maxCoverage * 100).ToString("N0"), "RemoteTech", body);
            }
        }
    }
}
