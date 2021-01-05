﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator.Util;
using KSP.Localization;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement for SCANsat coverage of a specific location.
    /// </summary>
    public class SCANsatLocationCoverageRequirement : ContractRequirement
    {
        protected string scanType;
        protected double latitude;
        protected double longitude;
        protected PQSCity pqsCity;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Before loading, verify the SCANsat version
            if (!SCANsatUtil.VerifySCANsatVersion())
            {
                return false;
            }

            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            // Do not check the requirement on active contracts.  Otherwise when they scan the
            // contract is invalidated, which is usually not what's meant.
            checkOnActiveContract = false;

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "scanType", x => scanType = x, this, "Anomaly", SCANsatUtil.ValidateSCANname);
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "latitude", x => latitude = x, this, 0.0);
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "longitude", x => longitude = x, this, 0.0);

            valid &= ConfigNodeUtil.MutuallyExclusive(configNode, new string[] { "latitude", "longitude" }, new string[] { "pqsCity" }, this);
            valid &= ValidateTargetBody(configNode);

            string pqsName = null;
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "pqsCity", x => pqsName = x, this, (string)null);
            if (pqsName != null)
            {
                try
                {
                    pqsCity = targetBody.GetComponentsInChildren<PQSCity>(true).Where(pqs => pqs.name == pqsName).First();
                }
                catch (Exception e)
                {
                    LoggingUtil.LogError(this, "Couldn't load PQSCity with name '{0}'", pqsCity);
                    LoggingUtil.LogException(e);
                    valid = false;
                }
            }

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("latitude", latitude);
            configNode.AddValue("longitude", longitude);
            configNode.AddValue("scanType", scanType);
            if (pqsCity != null)
            {
                configNode.AddValue("pqsCity", pqsCity.name);
            }
        }

        public override void OnLoad(ConfigNode configNode)
        {
            latitude = ConfigNodeUtil.ParseValue<double>(configNode, "latitude");
            longitude = ConfigNodeUtil.ParseValue<double>(configNode, "longitude");
            scanType = ConfigNodeUtil.ParseValue<string>(configNode, "scanType");
            string pqsCityName = ConfigNodeUtil.ParseValue<string>(configNode, "pqsCity", "");
            if (!string.IsNullOrEmpty(pqsCityName))
            {
                pqsCity = targetBody.GetComponentsInChildren<PQSCity>(true).Where(pqs => pqs.name == pqsCityName).First();
            }
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            // Perform another validation of the target body to catch late validation issues due to expressions
            if (!ValidateTargetBody())
            {
                return false;
            }

            if (pqsCity != null)
            {
                latitude = targetBody.GetLatitude(pqsCity.transform.position);
                longitude = targetBody.GetLongitude(pqsCity.transform.position);
                pqsCity = null;
            }

            return SCANsatUtil.IsCovered(latitude, longitude, SCANsatUtil.GetSCANtype(scanType), targetBody);
        }

        protected override string RequirementText()
        {
            string body = targetBody == null ? Localizer.GetStringByTag("#cc.req.ProgressCelestialBody.genericBody") : targetBody.displayName;
            string lat = StringBuilderCache.Format("<color=#{0}>{1}</color>", MissionControlUI.RequirementHighlightColor, latitude.ToString("N1"));
            string lon = StringBuilderCache.Format("<color=#{0}>{1}</color>", MissionControlUI.RequirementHighlightColor, longitude.ToString("N1"));

            return Localizer.Format(invertRequirement ? "#cc.scansat.req.SCANsatLocationCoverage.x" : "#cc.scansat.req.SCANsatLocationCoverage",
                SCANsatCoverage.ScanDisplayName(scanType), lat, lon, body);
        }
    }
}
